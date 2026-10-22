namespace SportFoglalasok;

public sealed class Foglalas
{
    public string Nev { get; set; } = string.Empty;
    public DateTime Datum { get; set; }
    public string Kezdes { get; set; } = string.Empty;
    public string Befejezes { get; set; } = string.Empty;
    public string Helyszin { get; set; } = string.Empty;
    public string Eszkozok { get; set; } = "Saját eszközt hoz";
}
