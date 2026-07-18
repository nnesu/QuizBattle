using QuizBattle.Models;
using QuizBattle.Services;
using System.Collections.ObjectModel;

namespace QuizBattle;

public partial class DecksPage : ContentPage
{
    private readonly DatabaseService _dbService;
    public ObservableCollection<DeckEntity> Decks { get; set; } = new();

    public DecksPage()
    {
        InitializeComponent();
        _dbService = new DatabaseService();
        DecksCollectionView.ItemsSource = Decks;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDecksAsync();
    }

    private async Task LoadDecksAsync()
    {
        var decksFromDb = await _dbService.GetDecksAsync();
        Decks.Clear();
        foreach (var deck in decksFromDb)
        {
            Decks.Add(deck);
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnCreateDeckClicked(object sender, EventArgs e)
    {
        string result = await DisplayPromptAsync("New Deck", "Enter deck name:", "CREATE", "CANCEL");

        if (!string.IsNullOrWhiteSpace(result))
        {
            await _dbService.CreateDeckAsync(result);
            await LoadDecksAsync();
        }
    }

    private async void OnSearchGlobalClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Coming Soon", "The Global Deck Search feature will be implemented here!", "OK");
    }

    private async void OnEditDeckClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is DeckEntity deck)
        {
            await DisplayAlert("Edit Mode", $"Opening editor for {deck.Name}...", "OK");
        }
    }

    private async void OnDeleteDeckClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is DeckEntity deck)
        {
            bool confirm = await DisplayAlert("Delete Deck", $"Are you sure you want to delete '{deck.Name}'? This cannot be undone.", "YES", "NO");
            if (confirm)
            {
                await _dbService.DeleteDeckAsync(deck.Id);
                await LoadDecksAsync();
            }
        }
    }
}