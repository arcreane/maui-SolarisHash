using MyApp.Models;
using MyApp.Services;

namespace MyApp.Views
{
    public class CardinalPlacesView : ContentView
    {
        private readonly ICardinalDirectionService _cardinalService;
        private StackLayout _mainLayout;
        private Location? _userLocation;

        public static readonly BindableProperty PlacesProperty = BindableProperty.Create(
            nameof(Places), 
            typeof(IEnumerable<Place>), 
            typeof(CardinalPlacesView), 
            null, 
            propertyChanged: OnPlacesChanged);

        public static readonly BindableProperty UserLocationProperty = BindableProperty.Create(
            nameof(UserLocation), 
            typeof(Location), 
            typeof(CardinalPlacesView), 
            null, 
            propertyChanged: OnUserLocationChanged);

        public IEnumerable<Place>? Places
        {
            get => (IEnumerable<Place>?)GetValue(PlacesProperty);
            set => SetValue(PlacesProperty, value);
        }

        public Location? UserLocation
        {
            get => (Location?)GetValue(UserLocationProperty);
            set => SetValue(UserLocationProperty, value);
        }

        public CardinalPlacesView()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🧭 CardinalPlacesView: Initialisation...");
                
                // ✅ CORRECTION: Créer le service directement (pas de DI)
                _cardinalService = new CardinalDirectionService();
                System.Diagnostics.Debug.WriteLine("🧭 CardinalDirectionService créé");
                
                CreateCardinalInterface();
                System.Diagnostics.Debug.WriteLine("🧭 Interface cardinale créée");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur CardinalPlacesView: {ex.Message}");
            }
        }

        private void CreateCardinalInterface()
        {
            _mainLayout = new StackLayout
            {
                Spacing = 16,
                Padding = 20
            };

            var titleLabel = new Label
            {
                Text = "🧭 Lieux par Direction Cardinale",
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Colors.DarkBlue,
                Margin = new Thickness(0, 0, 0, 16)
            };

            _mainLayout.Children.Add(titleLabel);

            // ✅ AJOUT: Message de debug visible
            var debugLabel = new Label
            {
                Text = "🔍 En attente de lieux...",
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Colors.Gray,
                FontAttributes = FontAttributes.Italic
            };
            _mainLayout.Children.Add(debugLabel);

            Content = new ScrollView
            {
                Content = new Frame
                {
                    Content = _mainLayout,
                    BackgroundColor = Colors.White,
                    BorderColor = Colors.LightGray,
                    CornerRadius = 16,
                    Padding = 0,
                    HasShadow = true
                }
            };
        }

        private static void OnPlacesChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is CardinalPlacesView view)
            {
                System.Diagnostics.Debug.WriteLine($"🧭 Places changées: {(newValue as IEnumerable<Place>)?.Count() ?? 0} lieux");
                view.UpdateCardinalDisplay();
            }
        }

        private static void OnUserLocationChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is CardinalPlacesView view && newValue is Location location)
            {
                System.Diagnostics.Debug.WriteLine($"🧭 Position changée: {location.Latitude:F4}, {location.Longitude:F4}");
                view._userLocation = location;
                view.UpdateCardinalDisplay();
            }
        }

        private void UpdateCardinalDisplay()
        {
            if (Places == null || _userLocation == null)
            {
                System.Diagnostics.Debug.WriteLine($"🧭 Pas prêt: Places={Places?.Count() ?? 0}, Location={_userLocation != null}");
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("🧭 Mise à jour affichage cardinal...");

                    // Supprimer les anciens groupes (garder le titre)
                    while (_mainLayout.Children.Count > 1)
                    {
                        _mainLayout.Children.RemoveAt(1);
                    }

                    var placesList = Places.ToList();
                    System.Diagnostics.Debug.WriteLine($"🧭 {placesList.Count} lieux à traiter");

                    if (!placesList.Any())
                    {
                        var noPlacesLabel = new Label
                        {
                            Text = "Aucun lieu à afficher",
                            HorizontalTextAlignment = TextAlignment.Center,
                            TextColor = Colors.Gray,
                            FontAttributes = FontAttributes.Italic
                        };
                        _mainLayout.Children.Add(noPlacesLabel);
                        return;
                    }

                    // ✅ TEST: Afficher d'abord un message simple
                    var testLabel = new Label
                    {
                        Text = $"✅ Traitement de {placesList.Count} lieux à {_userLocation.Latitude:F4}, {_userLocation.Longitude:F4}",
                        FontSize = 12,
                        TextColor = Colors.Green,
                        HorizontalTextAlignment = TextAlignment.Center
                    };
                    _mainLayout.Children.Add(testLabel);

                    // Grouper par direction cardinale
                    var cardinalGroups = _cardinalService.GroupPlacesByCardinalDirection(
                        placesList, 
                        _userLocation.Latitude, 
                        _userLocation.Longitude);

                    System.Diagnostics.Debug.WriteLine($"🧭 {cardinalGroups.Count} groupes cardinaux créés");

                    if (cardinalGroups.Any())
                    {
                        foreach (var group in cardinalGroups)
                        {
                            var groupFrame = CreateSimpleGroupFrame(group);
                            _mainLayout.Children.Add(groupFrame);
                        }
                    }
                    else
                    {
                        var noGroupsLabel = new Label
                        {
                            Text = "❌ Aucun groupe cardinal généré",
                            TextColor = Colors.Red,
                            HorizontalTextAlignment = TextAlignment.Center
                        };
                        _mainLayout.Children.Add(noGroupsLabel);
                    }

                    // Ajouter un résumé
                    var summaryLabel = new Label
                    {
                        Text = $"📊 Total: {placesList.Count} lieux en {cardinalGroups.Count} directions",
                        FontSize = 12,
                        HorizontalTextAlignment = TextAlignment.Center,
                        TextColor = Colors.DarkGreen,
                        FontAttributes = FontAttributes.Bold,
                        Margin = new Thickness(0, 16, 0, 0)
                    };
                    _mainLayout.Children.Add(summaryLabel);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Erreur mise à jour cardinal: {ex.Message}");
                    
                    var errorLabel = new Label
                    {
                        Text = $"❌ Erreur: {ex.Message}",
                        TextColor = Colors.Red,
                        HorizontalTextAlignment = TextAlignment.Center
                    };
                    _mainLayout.Children.Add(errorLabel);
                }
            });
        }

        // ✅ VERSION SIMPLIFIÉE pour debug
        private Frame CreateSimpleGroupFrame(CardinalGroup group)
        {
            var content = new StackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label
                    {
                        Text = $"{group.Emoji} {group.Direction} ({group.Count} lieux)",
                        FontSize = 16,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = GetDirectionColor(group.Direction),
                        HorizontalTextAlignment = TextAlignment.Center
                    }
                }
            };

            // Ajouter les 3 premiers lieux
            foreach (var place in group.Places.Take(3))
            {
                var placeLabel = new Label
                {
                    Text = $"• {place.Name} ({place.FormattedDistance})",
                    FontSize = 12,
                    TextColor = Colors.Black,
                    Margin = new Thickness(16, 0, 0, 0)
                };
                content.Children.Add(placeLabel);
            }

            if (group.Places.Count > 3)
            {
                var moreLabel = new Label
                {
                    Text = $"... et {group.Places.Count - 3} autres",
                    FontSize = 10,
                    FontAttributes = FontAttributes.Italic,
                    TextColor = Colors.Gray,
                    Margin = new Thickness(16, 0, 0, 0)
                };
                content.Children.Add(moreLabel);
            }

            return new Frame
            {
                Content = content,
                BackgroundColor = GetDirectionBackgroundColor(group.Direction),
                BorderColor = GetDirectionColor(group.Direction),
                CornerRadius = 12,
                Padding = 12,
                Margin = new Thickness(0, 4),
                HasShadow = false
            };
        }

        private Color GetDirectionColor(string direction)
        {
            return direction switch
            {
                "Nord" => Colors.Red,
                "Nord-Est" => Colors.Orange,
                "Est" => Colors.Gold,
                "Sud-Est" => Colors.Green,
                "Sud" => Colors.Blue,
                "Sud-Ouest" => Colors.Purple,
                "Ouest" => Colors.Magenta,
                "Nord-Ouest" => Colors.Teal,
                _ => Colors.Gray
            };
        }

        private Color GetDirectionBackgroundColor(string direction)
        {
            return direction switch
            {
                "Nord" => Color.FromRgb(255, 240, 240),
                "Nord-Est" => Color.FromRgb(255, 248, 220),
                "Est" => Color.FromRgb(255, 255, 224),
                "Sud-Est" => Color.FromRgb(240, 255, 240),
                "Sud" => Color.FromRgb(240, 248, 255),
                "Sud-Ouest" => Color.FromRgb(248, 240, 255),
                "Ouest" => Color.FromRgb(255, 240, 245),
                "Nord-Ouest" => Color.FromRgb(240, 255, 255),
                _ => Color.FromRgb(248, 248, 248)
            };
        }
    }
}