using System.Windows;
using GGPOLauncher.Core.Models;

namespace GGPOLauncher.Views;

public partial class MatchResultDialog : Window
{
    public MatchResultDialogDecision Decision { get; private set; }

    public MatchResultDialog(string kingDisplayName, string challengerDisplayName)
    {
        InitializeComponent();

        TxtPrompt.Text = $"Elige quién ganó entre {kingDisplayName} y {challengerDisplayName}. El resultado actualizará rey, cola y ranking.";
        BtnKingWon.Content = $"Ganó el rey: {kingDisplayName}";
        BtnChallengerWon.Content = $"Ganó el retador: {challengerDisplayName}";
    }

    private void BtnKingWon_Click(object sender, RoutedEventArgs e)
    {
        Decision = MatchResultDialogDecision.KingWon;
        Close();
    }

    private void BtnChallengerWon_Click(object sender, RoutedEventArgs e)
    {
        Decision = MatchResultDialogDecision.ChallengerWon;
        Close();
    }

    private void BtnCancelled_Click(object sender, RoutedEventArgs e)
    {
        Decision = MatchResultDialogDecision.Cancelled;
        Close();
    }

    private void BtnNeedsReview_Click(object sender, RoutedEventArgs e)
    {
        Decision = MatchResultDialogDecision.NeedsReview;
        Close();
    }
}