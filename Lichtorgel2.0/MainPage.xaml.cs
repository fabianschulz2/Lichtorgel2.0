using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.Media.Audio;
using System.Diagnostics;
using Windows.UI.Popups;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x407 dokumentiert.

namespace Lichtorgel2._0
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        List<String> Mp3List = new List<String>();
        public MainPage()
        {
            this.InitializeComponent();

            //Liste fuer Combobox erstellen
            Mp3List.Add("test.mp3");
            Mp3List.Add("test.wav");

        }

        private async void ButtonPlay_OnClick(object sender, RoutedEventArgs e)
        {
            if(songSelect.SelectedItem == null)
            {
                MessageDialog dialog = new MessageDialog("Bitte wähle eine Audiodatei aus");
                await dialog.ShowAsync();

            }
            else {
                Audio audio = new Audio();
                Debug.WriteLine(songSelect.SelectedItem.ToString());
                audio.Start(songSelect.SelectedItem.ToString());
            }

            
        }

    }
}
