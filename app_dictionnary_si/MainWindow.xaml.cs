using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

            public String txtCharPersonnalises { get; set; }

            //public String txtCheminFile { get; set; }

            public void generateurDic()
            {

            }
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

        private void btnGenerer_Click(object sender, RoutedEventArgs e)
        {
            Recup_data recup_Data = new Recup_data();

            recup_saisie(recup_Data);

        }


        public void recup_saisie(Recup_data recup_Data)
        {
            // controle de saisie
            if (!int.TryParse(txtLongueurMin.Text, out int min))
            {
                MessageBox.Show("La longueur minimale doit être un nombre entier.",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // controle de saisie
            if (!int.TryParse(txtLongueurMax.Text, out int max))
            {
                MessageBox.Show("La longueur maximale doit être un nombre entier.",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (min <= 0 || max <= 0 || min > max)
            {
                MessageBox.Show("Vérifiez que Min > 0 et Min ≤ Max.",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (chkMinuscules.IsChecked == false && chkMajuscules.IsChecked == false &&
                chkChiffres.IsChecked == false && chkSpeciaux.IsChecked == false &&
                string.IsNullOrEmpty(txtCaracteresPersonnalises.Text))
            {
                MessageBox.Show("Veuillez sélectionner au moins un type de caractère ou saisir des caractères personnalisés.",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }



            recup_Data.Longeurmin = min;
            recup_Data.Longeurmax = max;
            recup_Data.chkMinus = chkMinuscules.IsChecked == true;
            recup_Data.chkMajus = chkMajuscules.IsChecked == true;
            recup_Data.chkChiffre = chkChiffres.IsChecked == true;
            recup_Data.chkSpec = chkSpeciaux.IsChecked == true;
            recup_Data.txtCharPersonnalises = txtCaracteresPersonnalises.Text;
        }

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
            if (txtCaracteresPersonnalises != null)
            {
                chkMinuscules.IsEnabled = false;
                chkMajuscules.IsEnabled = false;
                chkChiffres.IsEnabled = false;
                chkSpeciaux.IsEnabled = false;
            }

            if (string.IsNullOrEmpty(txtCaracteresPersonnalises.Text))
            {
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





