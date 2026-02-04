using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
            public bool chkMajus { get; set;}
            public bool chkChiffre { get; set; }
            public bool chkSpec { get; set; }

            public String txtCharPersonnalises { get; set; }

            //public String txtCheminFile { get; set; }
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

			controle_saisie(recup_Data);
			
		}


        public void controle_saisie(Recup_data recup_Data)
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

			recup_Data.Longeurmin = min;
			recup_Data.Longeurmax = max;
		}
	}
}
