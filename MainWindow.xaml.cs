using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace SportFoglalasok;

public partial class MainWindow : Window
{
    private const string FileHeader = "# nev|datum|kezdes|befejezes|helyszin|eszkozok";
    private List<Foglalas> _bookings = [];
    private string? _filePath;

    public MainWindow()
    {
        InitializeComponent();
        BookingDatePicker.SelectedDate = DateTime.Today;
        LocationComboBox.SelectedIndex = 0;
        EquipmentOptionComboBox.SelectedIndex = 0;
        BookingsGrid.ItemsSource = _bookings;
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
            _filePath = dialog.FileName;
            _bookings = bookings;
            FilePathText.Text = dialog.FileName;
            FilePathText.ToolTip = dialog.FileName;
            BookingsGrid.ItemsSource = _bookings;

            StatusText.Text = bookings.Count == 0
                ? "A fájl még nem tartalmaz foglalást. A lista üres."
                : $"Betöltve: {bookings.Count} foglalás.";
        }
        catch (InvalidDataException ex)
        {
            ShowError("A kiválasztott TXT-fájl formátuma hibás:\n" + ex.Message, "Hibás adatfájl");
            StatusText.Text = "A fájl betöltése nem sikerült.";
        }
        catch (IOException ex)
        {
            ShowError($"A fájl beolvasása nem sikerült:\n{ex.Message}", "Fájlolvasási hiba");
            StatusText.Text = "A fájl betöltése nem sikerült.";
        }
        catch (UnauthorizedAccessException ex)
        {
            ShowError($"A fájl nem olvasható:\n{ex.Message}", "Hozzáférési hiba");
            StatusText.Text = "A fájl betöltése nem sikerült.";
        }
    }

    private void EquipmentOptionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EquipmentTextBox is not null && EquipmentOptionComboBox is not null)
            EquipmentTextBox.IsEnabled = EquipmentOptionComboBox.SelectedIndex == 1;
    }

    private void AddBookingButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_filePath))
        {
            ShowError("Előbb válassz ki egy TXT-adatfájlt.", "Nincs adatfájl");
            return;
        }

        var name = NameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("Add meg a foglaló nevét.", "Hiányzó adat");
            NameTextBox.Focus();
            return;
        }

        if (ContainsDelimiter(name))
        {
            ShowError("A mezőkben nem szerepelhet | karakter vagy sortörés.", "Nem használható karakter");
            return;
        }

        if (BookingDatePicker.SelectedDate is not DateTime date)
        {
            ShowError("Válassz ki egy dátumot.", "Hiányzó adat");
            return;
        }

        if (LocationComboBox.SelectedItem is not ComboBoxItem selectedLocation || selectedLocation.Content is not string location)
        {
            ShowError("Válassz ki egy helyszínt.", "Hiányzó adat");
            return;
        }

        if (!TimeOnly.TryParseExact(StartTimeTextBox.Text.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start)
            || !TimeOnly.TryParseExact(EndTimeTextBox.Text.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        {
            ShowError("Az időpontot HH:mm formátumban add meg, például 16:30.", "Hibás időpont");
            return;
        }

        if (end <= start)
        {
            ShowError("A befejezésnek később kell lennie a kezdésnél.", "Hibás időtartam");
            return;
        }

        var equipment = EquipmentOptionComboBox.SelectedIndex == 1
            ? EquipmentTextBox.Text.Trim()
            : "Saját eszközt hoz";

        if (EquipmentOptionComboBox.SelectedIndex == 1 && string.IsNullOrWhiteSpace(equipment))
        {
            ShowError("Írd be, milyen sporteszközt kérnek.", "Hiányzó adat");
            EquipmentTextBox.Focus();
            return;
        }

        if (ContainsDelimiter(equipment))
        {
            ShowError("A mezőkben nem szerepelhet | karakter vagy sortörés.", "Nem használható karakter");
            return;
        }

        var booking = new Foglalas
        {
            Nev = name,
            Datum = date.Date,
            Kezdes = start.ToString("HH:mm", CultureInfo.InvariantCulture),
            Befejezes = end.ToString("HH:mm", CultureInfo.InvariantCulture),
            Helyszin = location,
            Eszkozok = equipment
        };

        var collision = _bookings.FirstOrDefault(existing => Conflicts(existing, booking));
        if (collision is not null)
        {
            ShowError(
                $"Erre az időpontra már van foglalás ugyanazon a helyszínen vagy a terem érintett felén: {collision.Nev}, {collision.Kezdes}-{collision.Befejezes}.",
                "Időpontütközés");
            return;
        }

        try
        {
            AppendBooking(_filePath, booking);
            _bookings.Add(booking);
            BookingsGrid.Items.Refresh();
            StatusText.Text = $"A foglalás mentve. Összesen {_bookings.Count} foglalás.";
            ClearForm();
        }
        catch (IOException ex)
        {
            ShowError($"A foglalás mentése nem sikerült:\n{ex.Message}", "Fájlírási hiba");
        }
        catch (UnauthorizedAccessException ex)
        {
            ShowError($"A fájl nem írható:\n{ex.Message}", "Hozzáférési hiba");
        }
    }

    private static List<Foglalas> LoadBookings(string filePath)
    {
        if (!File.Exists(filePath))
            return [];

        var bookings = new List<Foglalas>();
        foreach (var line in File.ReadLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            var fields = line.Split('|');
            if (fields.Length != 6)
                throw new InvalidDataException("Minden foglalási sorban 6 mezőnek kell lennie, | jellel elválasztva.");

            if (!DateTime.TryParseExact(fields[1].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                throw new InvalidDataException($"Nem értelmezhető dátum: {fields[1]}");

            if (!TimeOnly.TryParseExact(fields[2].Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                || !TimeOnly.TryParseExact(fields[3].Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                throw new InvalidDataException($"Nem értelmezhető időpont a sorban: {line}");

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

    private static void AppendBooking(string filePath, Foglalas booking)
    {
        var existingText = File.Exists(filePath) ? File.ReadAllText(filePath) : string.Empty;
        var line = string.Join("|",
            booking.Nev,
            booking.Datum.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            booking.Kezdes,
            booking.Befejezes,
            booking.Helyszin,
            booking.Eszkozok);

        string textToAppend;
        if (string.IsNullOrWhiteSpace(existingText))
        {
            textToAppend = FileHeader + Environment.NewLine + line + Environment.NewLine;
        }
        else
        {
            var separator = existingText.EndsWith('\n') || existingText.EndsWith('\r') ? string.Empty : Environment.NewLine;
            textToAppend = separator + line + Environment.NewLine;
        }

        File.AppendAllText(filePath, textToAppend, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static bool Conflicts(Foglalas existing, Foglalas candidate)
    {
        if (existing.Datum.Date != candidate.Datum.Date || !LocationsOverlap(existing.Helyszin, candidate.Helyszin))
            return false;

        if (!TimeOnly.TryParseExact(existing.Kezdes, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var existingStart)
            || !TimeOnly.TryParseExact(existing.Befejezes, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var existingEnd)
            || !TimeOnly.TryParseExact(candidate.Kezdes, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var candidateStart)
            || !TimeOnly.TryParseExact(candidate.Befejezes, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var candidateEnd))
            return false;

        return candidateStart < existingEnd && candidateEnd > existingStart;
    }

    private static bool LocationsOverlap(string first, string second)
    {
        const string firstHalf = "Tornaterem - 1. fél";
        const string secondHalf = "Tornaterem - 2. fél";
        const string fullGym = "Tornaterem - teljes terem";

        if (first == fullGym)
            return second is firstHalf or secondHalf or fullGym;
        if (second == fullGym)
            return first is firstHalf or secondHalf;

        return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
    }

    private void ClearForm()
    {
        NameTextBox.Clear();
        StartTimeTextBox.Text = "16:00";
        EndTimeTextBox.Text = "17:00";
        EquipmentTextBox.Clear();
        EquipmentOptionComboBox.SelectedIndex = 0;
        BookingDatePicker.SelectedDate = DateTime.Today;
        NameTextBox.Focus();
    }

    private static bool ContainsDelimiter(string value) => value.Contains('|') || value.Contains('\r') || value.Contains('\n');

    private void ShowError(string message, string title)
    {
        MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        StatusText.Text = message.Replace('\n', ' ');
    }
}
