using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace SportFoglalasok;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        BookingsGrid.ItemsSource = Array.Empty<Foglalas>();
    }

    private void SelectFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Foglalási adatfájl kiválasztása",
            Filter = "Szövegfájl (*.txt)|*.txt|Minden fájl (*.*)|*.*",
            DefaultExt = ".txt",
            AddExtension = true,
            FileName = "foglalasok.txt",
            CheckPathExists = true,
            OverwritePrompt = false
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            var bookings = LoadBookings(dialog.FileName);
            FilePathText.Text = dialog.FileName;
            FilePathText.ToolTip = dialog.FileName;
            BookingsGrid.ItemsSource = bookings;

            StatusText.Text = bookings.Count == 0
                ? "A fájl még nem tartalmaz foglalást. A lista üres."
                : $"Betöltve: {bookings.Count} foglalás.";
        }
        catch (InvalidDataException ex)
        {
            MessageBox.Show(
                this,
                $"A kiválasztott TXT-fájl formátuma hibás:\n{ex.Message}",
                "Hibás adatfájl",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            StatusText.Text = "A fájl betöltése nem sikerült.";
        }
        catch (IOException ex)
        {
            MessageBox.Show(
                this,
                $"A fájl beolvasása nem sikerült:\n{ex.Message}",
                "Fájlolvasási hiba",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            StatusText.Text = "A fájl betöltése nem sikerült.";
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show(
                this,
                $"A fájl nem olvasható:\n{ex.Message}",
                "Hozzáférési hiba",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            StatusText.Text = "A fájl betöltése nem sikerült.";
        }
    }

    private static List<Foglalas> LoadBookings(string filePath)
    {
        if (!File.Exists(filePath))
            return [];

        var lines = File.ReadAllLines(filePath);
        if (lines.Length == 0)
            return [];

        var bookings = new List<Foglalas>();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            var fields = line.Split('|');
            if (fields.Length != 6)
                throw new InvalidDataException("Minden foglalási sorban 6 mezőnek kell lennie, | jellel elválasztva.");

            if (!DateTime.TryParse(fields[1], out var date))
                throw new InvalidDataException($"Nem értelmezhető dátum: {fields[1]}");

            bookings.Add(new Foglalas
            {
                Nev = fields[0].Trim(),
                Datum = date,
                Kezdes = fields[2].Trim(),
                Befejezes = fields[3].Trim(),
                Helyszin = fields[4].Trim(),
                Eszkozok = fields[5].Trim()
            });
        }

        return bookings;
    }
}
