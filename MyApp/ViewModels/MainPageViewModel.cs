using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Devices.Sensors;
using MyApp.Models;
using MyApp.Services;

namespace MyApp.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        private readonly IPlaceService _placeService;
        private readonly ILocationService _locationService;
        private readonly IOrientationService? _orientationService;

        // ✅ Thread-safe observable properties with explicit backing fields
        private bool _isLoading;
        private bool _isLocationEnabled;
        private bool _isOrientationFilterEnabled;
        private string _currentLocation = "📍 Localisation en cours...";
        private string _currentOrientation = "📱 Orientation inconnue";
        private string _searchQuery = string.Empty;
        private string _selectedFilter = "Tous";
        private string _statusMessage = "Prêt à chercher des lieux...";
        private string _selectedCityName = string.Empty;

        // ✅ Thread-safe properties with command state updates
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    // ✅ Update command states on main thread
                    if (MainThread.IsMainThread)
                    {
                        LoadPlacesCommand?.NotifyCanExecuteChanged();
                        GoToCityCommand?.NotifyCanExecuteChanged();
                        RefreshLocationCommand?.NotifyCanExecuteChanged();
                        ToggleOrientationFilterCommand?.NotifyCanExecuteChanged();
                        SearchPlacesCommand?.NotifyCanExecuteChanged();
                        FilterChangedCommand?.NotifyCanExecuteChanged();
                        CitySelectedCommand?.NotifyCanExecuteChanged();
                    }
                    else
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            LoadPlacesCommand?.NotifyCanExecuteChanged();
                            GoToCityCommand?.NotifyCanExecuteChanged();
                            RefreshLocationCommand?.NotifyCanExecuteChanged();
                            ToggleOrientationFilterCommand?.NotifyCanExecuteChanged();
                            SearchPlacesCommand?.NotifyCanExecuteChanged();
                            FilterChangedCommand?.NotifyCanExecuteChanged();
                            CitySelectedCommand?.NotifyCanExecuteChanged();
                        });
                    }
                }
            }
        }

        public bool IsLocationEnabled
        {
            get => _isLocationEnabled;
            set => SetProperty(ref _isLocationEnabled, value);
        }

        public bool IsOrientationFilterEnabled
        {
            get => _isOrientationFilterEnabled;
            set => SetProperty(ref _isOrientationFilterEnabled, value);
        }

        public string CurrentLocation
        {
            get => _currentLocation;
            set => SetProperty(ref _currentLocation, value);
        }

        public string CurrentOrientation
        {
            get => _currentOrientation;
            set => SetProperty(ref _currentOrientation, value);
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set => SetProperty(ref _searchQuery, value);
        }

        public string SelectedFilter
        {
            get => _selectedFilter;
            set => SetProperty(ref _selectedFilter, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string SelectedCityName
        {
            get => _selectedCityName;
            set
            {
                if (SetProperty(ref _selectedCityName, value))
                {
                    // ✅ Update GoToCityCommand state when city name changes
                    if (MainThread.IsMainThread)
                    {
                        GoToCityCommand?.NotifyCanExecuteChanged();
                    }
                    else
                    {
                        MainThread.BeginInvokeOnMainThread(() => GoToCityCommand?.NotifyCanExecuteChanged());
                    }
                }
            }
        }

        public ObservableCollection<Place> Places { get; } = new();
        public ObservableCollection<string> FilterOptions { get; } = new()
        {
            "Tous", "Tourisme", "Restaurants", "Monuments", "Musées", "Parcs", "Services", "Commerce"
        };

        public ObservableCollection<string> PopularCities { get; } = new()
        {
            "Paris", "Lyon", "Marseille", "Toulouse", "Nice", "Nantes", "Montpellier", "Strasbourg", "Bordeaux", "Lille"
        };

        // ✅ Explicit command properties with proper CanExecute
        public IAsyncRelayCommand LoadPlacesCommand { get; }
        public IAsyncRelayCommand GoToCityCommand { get; }
        public IAsyncRelayCommand RefreshLocationCommand { get; }
        public IAsyncRelayCommand ToggleOrientationFilterCommand { get; }
        public IAsyncRelayCommand SearchPlacesCommand { get; }
        public IAsyncRelayCommand FilterChangedCommand { get; }
        public IAsyncRelayCommand<object> CitySelectedCommand { get; }
        public IAsyncRelayCommand<Place> PlaceSelectedCommand { get; }

        private Location? _currentLocationCoords;
        private List<Place> _allPlaces = new();

        public MainPageViewModel(IPlaceService placeService, ILocationService locationService, IOrientationService orientationService)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🚀 MainPageViewModel: Initialisation...");
                
                _placeService = placeService ?? throw new ArgumentNullException(nameof(placeService));
                _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
                _orientationService = orientationService;

                // ✅ Initialize commands with proper CanExecute predicates
                LoadPlacesCommand = new AsyncRelayCommand(LoadPlacesAsync, () => !IsLoading);
                GoToCityCommand = new AsyncRelayCommand(GoToCityAsync, () => !IsLoading && !string.IsNullOrWhiteSpace(SelectedCityName));
                RefreshLocationCommand = new AsyncRelayCommand(RefreshLocationAsync, () => !IsLoading);
                ToggleOrientationFilterCommand = new AsyncRelayCommand(ToggleOrientationFilterAsync, () => !IsLoading);
                SearchPlacesCommand = new AsyncRelayCommand(SearchPlacesAsync, () => !IsLoading);
                FilterChangedCommand = new AsyncRelayCommand(FilterChangedAsync, () => !IsLoading);
                CitySelectedCommand = new AsyncRelayCommand<object>(CitySelectedAsync, _ => !IsLoading);
                PlaceSelectedCommand = new AsyncRelayCommand<Place>(PlaceSelectedAsync, _ => !IsLoading);
                
                // S'abonner aux changements
                if (_locationService != null)
                {
                    _locationService.LocationChanged += OnLocationChanged;
                    System.Diagnostics.Debug.WriteLine("✅ LocationService connecté");
                }
                
                if (_orientationService != null)
                {
                    _orientationService.OrientationChanged += OnOrientationChanged;
                    System.Diagnostics.Debug.WriteLine("✅ OrientationService connecté");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ OrientationService non disponible");
                }
                
                System.Diagnostics.Debug.WriteLine($"🔍 Service utilisé: {_placeService.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"📍 Service de localisation: {_locationService.GetType().Name}");
                
                // ✅ Mise à jour UI thread-safe
                _ = MainThread.InvokeOnMainThreadAsync(() =>
                {
                    StatusMessage = "✅ TravelBuddy initialisé avec succès !";
                });
                
                System.Diagnostics.Debug.WriteLine("✅ MainPageViewModel: Initialisation terminée");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ MainPageViewModel: Erreur initialisation - {ex.Message}");
                
                _ = MainThread.InvokeOnMainThreadAsync(() =>
                {
                    StatusMessage = $"❌ Erreur d'initialisation: {ex.Message}";
                });
                throw;
            }
        }

        // ✅ Hide base property change notifications to ensure main thread
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (MainThread.IsMainThread)
            {
                base.OnPropertyChanged(propertyName);
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() => base.OnPropertyChanged(propertyName));
            }
        }

        private async Task LoadPlacesAsync()
        {
            if (IsLoading) return;

            try
            {
                // ✅ UI updates on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsLoading = true;
                    StatusMessage = "🔍 Recherche en cours...";
                });
                
                System.Diagnostics.Debug.WriteLine("🚀 Début de LoadPlacesAsync");
                
                // Vérifier d'abord si on a déjà une position définie
                if (_currentLocationCoords == null)
                {
                    await GetCurrentLocationAsync();
                }
                
                if (_currentLocationCoords != null)
                {
                    System.Diagnostics.Debug.WriteLine($"📍 Position utilisée pour recherche: {_currentLocationCoords.Latitude:F6}, {_currentLocationCoords.Longitude:F6}");
                    
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        StatusMessage = "🌐 Recherche de lieux réels...";
                    });
                    
                    var places = await _placeService.GetNearbyPlacesAsync(
                        _currentLocationCoords.Latitude,
                        _currentLocationCoords.Longitude,
                        string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery,
                        radius: 3000,
                        limit: 100
                    );

                    System.Diagnostics.Debug.WriteLine($"🏠 Lieux trouvés depuis API: {places.Count}");
                    
                    // Stocker tous les lieux
                    _allPlaces = places;

                    // Appliquer les filtres
                    var filteredPlaces = ApplyAllFilters(_allPlaces);

                    // ✅ ONLY modify Places collection on main thread
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Places.Clear();
                        foreach (var place in filteredPlaces.Take(50))
                        {
                            Places.Add(place);
                        }
                        
                        if (Places.Any())
                        {
                            StatusMessage = $"✅ {Places.Count} lieux trouvés près de {GetLocationName()}";
                        }
                        else
                        {
                            StatusMessage = $"⚠️ Aucun lieu trouvé près de {GetLocationName()} (rayon 3km)";
                        }
                    });
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        StatusMessage = "❌ Impossible d'obtenir votre position";
                    });
                    System.Diagnostics.Debug.WriteLine("❌ _currentLocationCoords est null");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 Erreur dans LoadPlacesAsync: {ex.Message}");
                
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    StatusMessage = $"❌ Erreur: {ex.Message}";
                    
                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Erreur", 
                            $"Impossible de charger les lieux:\n{ex.Message}", 
                            "OK"
                        );
                    }
                });
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }

        private async Task GoToCityAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedCityName) || IsLoading)
                return;

            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsLoading = true;
                    StatusMessage = $"🏙️ Recherche de {SelectedCityName}...";
                });

                System.Diagnostics.Debug.WriteLine($"🏙️ Recherche ville: {SelectedCityName}");

                var location = await _locationService.GetLocationByNameAsync(SelectedCityName);

                if (location != null)
                {
                    _currentLocationCoords = location;
                    
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        CurrentLocation = $"🏙️ {SelectedCityName} ({location.Latitude:F4}, {location.Longitude:F4})";
                        IsLocationEnabled = true;
                        Places.Clear();
                        StatusMessage = $"📍 Position mise à jour pour {SelectedCityName}";
                    });

                    System.Diagnostics.Debug.WriteLine($"✅ Coordonnées trouvées pour {SelectedCityName}: {location.Latitude:F6}, {location.Longitude:F6}");

                    _allPlaces.Clear();
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        StatusMessage = $"❌ Ville '{SelectedCityName}' non trouvée";
                    });
                    System.Diagnostics.Debug.WriteLine($"❌ Ville non trouvée: {SelectedCityName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur GoToCityAsync: {ex.Message}");
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    StatusMessage = $"❌ Erreur recherche ville: {ex.Message}";
                });
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }

        private async Task CitySelectedAsync(object selectedItem)
        {
            if (selectedItem is string cityName)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SelectedCityName = cityName;
                });
                await GoToCityAsync();
            }
        }

        private async Task SearchPlacesAsync()
        {
            System.Diagnostics.Debug.WriteLine($"🔍 Recherche avec query: '{SearchQuery}'");
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                StatusMessage = "🔍 Recherche avec filtre...";
            });
            
            await LoadPlacesAsync();
        }

        private async Task FilterChangedAsync()
        {
            System.Diagnostics.Debug.WriteLine($"🔽 Filtre changé vers: '{SelectedFilter}'");
            
            if (_allPlaces.Any())
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    StatusMessage = $"🔽 Filtrage par: {SelectedFilter}";
                });
                await RefreshPlacesAsync();
            }
        }

        private async Task RefreshLocationAsync()
        {
            System.Diagnostics.Debug.WriteLine("🔄 Actualisation de la position demandée");
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                StatusMessage = "📍 Actualisation de la position...";
                Places.Clear();
                SelectedCityName = string.Empty;
            });

            // Réinitialiser complètement la position
            _currentLocationCoords = null;
            _allPlaces.Clear();

            await GetCurrentLocationAsync();
        }

        private async Task ToggleOrientationFilterAsync()
        {
            if (_orientationService == null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    StatusMessage = "❌ Service d'orientation non disponible";
                });
                return;
            }

            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsOrientationFilterEnabled = !IsOrientationFilterEnabled;
                });
                
                if (IsOrientationFilterEnabled)
                {
                    await _orientationService.StartAsync();
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        StatusMessage = "🧭 Filtrage par orientation activé - Pointez votre téléphone !";
                    });
                    System.Diagnostics.Debug.WriteLine("🧭 Service d'orientation démarré");
                }
                else
                {
                    await _orientationService.StopAsync();
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        StatusMessage = "🧭 Filtrage par orientation désactivé";
                    });
                    System.Diagnostics.Debug.WriteLine("🛑 Service d'orientation arrêté");
                }
                
                // Reappliquer les filtres
                await RefreshPlacesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur ToggleOrientationFilter: {ex.Message}");
                
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    StatusMessage = $"❌ Erreur orientation: {ex.Message}";
                    
                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Erreur capteurs", 
                            $"Impossible d'activer l'orientation:\n{ex.Message}", 
                            "OK"
                        );
                    }
                });
            }
        }

        private async Task PlaceSelectedAsync(Place selectedPlace)
        {
            if (selectedPlace != null && Application.Current?.MainPage != null)
            {
                System.Diagnostics.Debug.WriteLine($"📍 Lieu sélectionné: {selectedPlace.Name}");
                
                var details = new List<string>
                {
                    $"📍 {selectedPlace.Address}",
                    $"📏 Distance: {selectedPlace.FormattedDistance}",
                    $"🏷️ Catégorie: {selectedPlace.MainCategory}"
                };
                
                if (!string.IsNullOrEmpty(selectedPlace.Description))
                    details.Insert(0, $"📝 {selectedPlace.Description}");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert(
                        selectedPlace.Name,
                        string.Join("\n\n", details),
                        "OK"
                    );
                });
            }
        }

        private async Task GetCurrentLocationAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("📍 Début de géolocalisation...");

                var location = await _locationService.GetCurrentLocationAsync();

                if (location != null)
                {
                    _currentLocationCoords = location;
                    
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        CurrentLocation = $"📍 {location.Latitude:F6}, {location.Longitude:F6}";
                        IsLocationEnabled = true;
                    });

                    System.Diagnostics.Debug.WriteLine($"✅ Position obtenue: {location.Latitude:F6}, {location.Longitude:F6}");
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        CurrentLocation = "❌ Localisation indisponible";
                        IsLocationEnabled = false;
                    });
                    System.Diagnostics.Debug.WriteLine("❌ Location est null");
                }
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    CurrentLocation = $"❌ Erreur: {ex.Message}";
                    IsLocationEnabled = false;
                });
                System.Diagnostics.Debug.WriteLine($"❌ Exception géolocalisation: {ex.Message}");
            }
        }

        private async Task RefreshPlacesAsync()
        {
            if (_allPlaces.Any() && _currentLocationCoords != null)
            {
                var filteredPlaces = ApplyAllFilters(_allPlaces);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Places.Clear();
                    foreach (var place in filteredPlaces.Take(50))
                    {
                        Places.Add(place);
                    }
                    StatusMessage = $"✅ {Places.Count} lieux trouvés près de {GetLocationName()}";
                });
            }
        }

        private List<Place> ApplyAllFilters(List<Place> places)
        {
            var filtered = places;
            
            // Filtre par catégorie
            filtered = ApplyFilter(filtered);
            
            // Filtre par orientation si activé
            if (IsOrientationFilterEnabled && _currentLocationCoords != null && _orientationService != null)
            {
                filtered = _orientationService.FilterPlacesByOrientation(
                    filtered, 
                    _currentLocationCoords.Latitude, 
                    _currentLocationCoords.Longitude,
                    tolerance: 60.0
                );
            }
            
            return filtered.OrderBy(p => p.Distance).ToList();
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            // ✅ Use InvokeOnMainThreadAsync consistently
            _ = MainThread.InvokeOnMainThreadAsync(() =>
            {
                _currentLocationCoords = e.NewLocation;
                CurrentLocation = $"📍 {e.NewLocation.Latitude:F6}, {e.NewLocation.Longitude:F6} ({e.Source})";
                IsLocationEnabled = true;
                
                System.Diagnostics.Debug.WriteLine($"📍 Localisation mise à jour: {e.NewLocation.Latitude:F6}, {e.NewLocation.Longitude:F6} via {e.Source}");
            });
        }

        private void OnOrientationChanged(object? sender, OrientationChangedEventArgs e)
        {
            // ✅ Use InvokeOnMainThreadAsync for proper async/await
            _ = MainThread.InvokeOnMainThreadAsync(async () =>
            {
                CurrentOrientation = $"📱 {e.DirectionName} ({e.Heading:F0}°)";
                if (IsOrientationFilterEnabled && _allPlaces.Any() && _currentLocationCoords != null)
                {
                    System.Diagnostics.Debug.WriteLine($"🧭 Orientation changée: {e.Heading:F0}° - Mise à jour des lieux");
                    await RefreshPlacesAsync();
                }
            });
        }

        private List<Place> ApplyFilter(List<Place> places)
        {
            if (SelectedFilter == "Tous" || string.IsNullOrEmpty(SelectedFilter))
                return places;

            var filtered = places.Where(p => 
            {
                var category = p.MainCategory.ToLower();
                var filter = SelectedFilter.ToLower();
                
                return filter switch
                {
                    "tourisme" => !string.IsNullOrEmpty(p.Tourism) || category.Contains("tourisme") || category.Contains("attraction"),
                    "restaurants" => !string.IsNullOrEmpty(p.Amenity) && (p.Amenity.Contains("restaurant") || p.Amenity.Contains("cafe") || p.Amenity.Contains("bar")),
                    "monuments" => !string.IsNullOrEmpty(p.Historic) || category.Contains("monument") || category.Contains("historique"),
                    "musées" => category.Contains("musée") || (!string.IsNullOrEmpty(p.Tourism) && p.Tourism.Contains("museum")),
                    "parcs" => category.Contains("parc") || category.Contains("jardin") || (!string.IsNullOrEmpty(p.Leisure) && (p.Leisure.Contains("park") || p.Leisure.Contains("garden"))),
                    "services" => !string.IsNullOrEmpty(p.Amenity) && !p.Amenity.Contains("restaurant") && !p.Amenity.Contains("cafe"),
                    "commerce" => !string.IsNullOrEmpty(p.Shop) || category.Contains("commerce"),
                    _ => category.Contains(filter)
                };
            }).ToList();
            
            return filtered;
        }

        private string GetLocationName()
        {
            if (!string.IsNullOrEmpty(SelectedCityName))
                return SelectedCityName;
            
            if (_currentLocationCoords != null)
                return "coordonnées actuelles";
            
            return "position inconnue";
        }

        public void Dispose()
        {
            try
            {
                if (_locationService != null)
                {
                    _locationService.LocationChanged -= OnLocationChanged;
                }

                if (_orientationService != null)
                {
                    _orientationService.OrientationChanged -= OnOrientationChanged;
                    Task.Run(async () => await _orientationService.StopAsync());
                }

                System.Diagnostics.Debug.WriteLine("✅ MainPageViewModel: Ressources nettoyées");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur dispose: {ex.Message}");
            }
        }
    }
}