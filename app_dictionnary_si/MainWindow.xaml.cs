using Microsoft.Win32;
using System;
using System.IO;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Concurrent;

namespace app_dictionnary_si
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public class Recup_data
        {
            public int Longeurmin { get; set; }
            public int Longeurmax { get; set; }
            public bool chkMinus { get; set; }
            public bool chkMajus { get; set; }
            public bool chkChiffre { get; set; }
            public bool chkSpec { get; set; }
            public string txtCharPersonnalises { get; set; }

            public void generateurDic() { }
        }

        private void btnParcourir_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Fichier texte (*.txt)|*.txt",
                DefaultExt = ".txt",
                FileName = "pass.txt"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                txtCheminFichier.Text = saveFileDialog.FileName;
            }
        }

        private async void btnGenerer_Click(object sender, RoutedEventArgs e)
        {
            // --- Validation et Récupération des données ---

            // controle de saisie
            if (!int.TryParse(txtLongueurMin.Text, out int min))
            {
                MessageBox.Show("La longueur minimale doit être un nombre entier.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // controle de saisie
            if (!int.TryParse(txtLongueurMax.Text, out int max))
            {
                MessageBox.Show("La longueur maximale doit être un nombre entier.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (min <= 0 || max <= 0 || min > max)
            {
                MessageBox.Show("Vérifiez que Min > 0 et Min ≤ Max.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string charset = GetCharset(); // Récupère la liste finale des caractères
            if (string.IsNullOrEmpty(charset))
            {
                MessageBox.Show("Veuillez sélectionner au moins un type de caractère ou saisir des caractères personnalisés.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string path = txtCheminFichier.Text;
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Veuillez choisir un chemin de fichier valide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // --- Préparation de l'interface ---

            btnGenerer.IsEnabled = false;
            ProgressBar.Value = 0;

            // Estimation du nombre total de combinaisons
            double total = 0;
            for (int i = min; i <= max; i++) total += Math.Pow(charset.Length, i);
            LblTotal.Text = $"Total estimé: {total:N0}";

            // --- Lancement du Worker ---

            try
            {
                // Lancer la génération sur un thread séparé pour ne pas geler l'interface
                await Task.Run(() => GenerateWorker(charset, min, max, path, total));

                MessageBox.Show("Génération terminée avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnGenerer.IsEnabled = true;
            }
        }

        // Méthode pour consolider les caractères choisis
        private string GetCharset()
        {
            // Si le champ personnalisé est utilisé, on ignore les cases à cocher
            if (!string.IsNullOrEmpty(txtCaracteresPersonnalises.Text))
            {
                return txtCaracteresPersonnalises.Text;
            }

            var sb = new StringBuilder();
            if (chkMinuscules.IsChecked == true) sb.Append("abcdefghijklmnopqrstuvwxyz");
            if (chkMajuscules.IsChecked == true) sb.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            if (chkChiffres.IsChecked == true) sb.Append("0123456789");
            if (chkSpeciaux.IsChecked == true) sb.Append("#$%?&*");

            return sb.ToString();
        }

        // --- Logique principale de génération ---

        private async void GenerateWorker(string charset, int min, int max, string path, double total)
        {
            long count = 0;
            var startTime = DateTime.Now;

            // Pré-calcul des octets pour éviter le CPU overhead
            byte[][] charsetBytes = new byte[charset.Length][];
            for (int i = 0; i < charset.Length; i++)
            {
                charsetBytes[i] = Encoding.UTF8.GetBytes(new char[] { charset[i] });
            }

            byte[] newlineBytes = Encoding.UTF8.GetBytes(Environment.NewLine);

            // Config du Double Buffering
            // Limite à 10 buffers en mémoire pour éviter de saturer la RAM si le disque est lent
            var queue = new BlockingCollection<byte[]>(boundedCapacity: 10);

            // Taille du buffer (4 Mo)
            int bufferSize = 4 * 1024 * 1024;

            // --- TÂCHE 1 : Consumer ---
            // Ce thread ne fait qu'attendre des données et les écrire sur le disque.
            var writerTask = Task.Run(() =>
            {
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.SequentialScan))
                {
                    foreach (var chunk in queue.GetConsumingEnumerable())
                    {
                        fs.Write(chunk, 0, chunk.Length);
                    }
                }
            });

            // --- TÂCHE 2 : Producer ---
            // Ce thread remplit les buffers.
            try
            {
                // On prépare le premier buffer
                byte[] currentBuffer = new byte[bufferSize];
                int bufferPos = 0;

                for (int len = min; len <= max; len++)
                {
                    long combinations = (long)Math.Pow(charset.Length, len);
                    int[] indices = new int[len];

                    // Calcul de la taille max d'une ligne pour vérifier l'espace buffer
                    int maxLineSize = (len * 4) + newlineBytes.Length;

                    for (long i = 0; i < combinations; i++)
                    {
                        // 1. Vérifier si le buffer est plein
                        if (bufferPos + maxLineSize > bufferSize)
                        {
                            // Envoie le buffer à l'autre thread
                            // Redimensionne le tableau à la taille exacte utile pour l'écriture propre
                            byte[] dataToSend = new byte[bufferPos];
                            Array.Copy(currentBuffer, dataToSend, bufferPos);

                            // Peut bloquer si le disque est trop lent
                            queue.Add(dataToSend);

                            // Reset du buffer local
                            bufferPos = 0;
                        }

                        // 2. Copie binaire
                        for (int k = 0; k < len; k++)
                        {
                            byte[] charBytes = charsetBytes[indices[k]];
                            // Copie manuelle byte par byte
                            if (charBytes.Length == 1)
                            {
                                currentBuffer[bufferPos++] = charBytes[0];
                            }
                            else
                            {
                                for (int b = 0; b < charBytes.Length; b++) currentBuffer[bufferPos++] = charBytes[b];
                            }
                        }

                        // Ajout du saut de ligne
                        for (int n = 0; n < newlineBytes.Length; n++)
                        {
                            currentBuffer[bufferPos++] = newlineBytes[n];
                        }

                        count++;

                        // 3. Incrémentation des indices
                        for (int k = len - 1; k >= 0; k--)
                        {
                            indices[k]++;
                            if (indices[k] < charset.Length) break;
                            indices[k] = 0;
                        }

                        // Mise à jour UI
                        if ((count & 65535) == 0)
                        {
                            UpdateStats(count, startTime, total);
                        }
                    }
                }

                // Envoyer le dernier buffer partiel s'il reste des données
                if (bufferPos > 0)
                {
                    byte[] lastChunk = new byte[bufferPos];
                    Array.Copy(currentBuffer, lastChunk, bufferPos);
                    queue.Add(lastChunk);
                }
            }
            finally
            {
                // Signaler au thread d'écriture qu'on a fini
                queue.CompleteAdding();
            }

            // Attendre que l'écriture soit totalement finie sur le disque
            await writerTask;

            UpdateStats(count, startTime, total);
        }

        private void UpdateStats(long count, DateTime startTime, double total)
        {
            Dispatcher.Invoke(() =>
            {
                var elapsed = DateTime.Now - startTime;
                // Protection division par zéro
                double seconds = elapsed.TotalSeconds > 0.001 ? elapsed.TotalSeconds : 0.001;

                double speed = count / seconds;

                string etaStr = "--:--";
                if (speed > 0 && total > count)
                {
                    double secondsRemaining = (total - count) / speed;
                    // On limite l'affichage pour éviter des bugs visuels sur les très gros nombres
                    if (secondsRemaining < 86400 * 100)
                    {
                        etaStr = TimeSpan.FromSeconds(secondsRemaining).ToString(@"hh\:mm\:ss");
                    }
                    else
                    {
                        etaStr = "> 100j";
                    }
                }

                // Formatage des nombres
                LblStats.Text = $"Mots: {count:N0} | {speed:N0}/sec | Temps: {elapsed:mm\\:ss} | Fin: {etaStr}";

                double pct = total > 0 ? (count / total) * 100.0 : 0;
                ProgressBar.Value = Math.Min(pct, 100);
            });
        }

        // --- Logique d'interface (Gestion des Checkbox) ---

        private void UpdateStatut(object sender, RoutedEventArgs e)
        {
            if (txtCaracteresPersonnalises == null) return;

            bool isStandardOptionSelected = PnlCaracteres.Children
                .OfType<CheckBox>()
                .Any(cb => cb.IsChecked == true);

            if (isStandardOptionSelected)
            {
                txtCaracteresPersonnalises.IsEnabled = false;
                txtCaracteresPersonnalises.Clear();
                txtCaracteresPersonnalises.Background = Brushes.LightGray;
            }
            else
            {
                txtCaracteresPersonnalises.IsEnabled = true;
                txtCaracteresPersonnalises.Background = Brushes.White;
            }
        }

        private void txtCaracteresPersonnalises_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            // Protection contre null au chargement initial
            if (txtCaracteresPersonnalises == null || chkMinuscules == null) return;

            if (!string.IsNullOrEmpty(txtCaracteresPersonnalises.Text))
            {
                // Désactivation des checkbox si du texte est saisi
                chkMinuscules.IsEnabled = false;
                chkMajuscules.IsEnabled = false;
                chkChiffres.IsEnabled = false;
                chkSpeciaux.IsEnabled = false;

                // Décoche tout visuellement
                chkMinuscules.IsChecked = false;
                chkMajuscules.IsChecked = false;
                chkChiffres.IsChecked = false;
                chkSpeciaux.IsChecked = false;
            }
            else
            {
                // Réactivation si le champ est vide
                chkMinuscules.IsEnabled = true;
                chkMajuscules.IsEnabled = true;
                chkChiffres.IsEnabled = true;
                chkSpeciaux.IsEnabled = true;
            }



        }

        private void txtCheminFichier_TextChanged(object sender, TextChangedEventArgs e)
        {
            majBoutonGenerer();
        }

        private void majBoutonGenerer()
        {
            bool valide = false;

            try
            {
                var chemin = txtCheminFichier?.Text;

                //Si le champ texte n'est pas vide, 
                if (!string.IsNullOrWhiteSpace(chemin))
                {
                    // on vérifie que le chemin est valide

                    // Specification du namespace System.IO pour éviter les conflits avec la classe Windows.Shapes du même nom
                    var dir = System.IO.Path.GetDirectoryName(chemin);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        valide = true;
                    }
                    else
                    {
                        valide = false;
                    }
                }
            }
            catch
            {
                valide = false;
            }

            //Le if est nécessaire pour éviter une NullReferenceException si le bouton n'est pas encore initialisé
            if (btnGenerer != null)
                btnGenerer.IsEnabled = valide;
        }
    }
}





