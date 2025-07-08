using Microsoft.Maui.Media;

namespace MyApp.Views
{
    public class CameraLocationView : ContentView
    {
        private Image _photoImage;
        private Label _statusLabel;
        private Button _takePhotoButton;
        private Button _choosePhotoButton;
        private Button _shareButton;
        private StackLayout _photoContainer;
        
        private string? _currentPhotoPath;

        public CameraLocationView()
        {
            CreateCameraInterface();
        }

        private void CreateCameraInterface()
        {
            // Image pour afficher la photo
            _photoImage = new Image
            {
                HeightRequest = 200,
                WidthRequest = 300,
                Aspect = Aspect.AspectFit,
                BackgroundColor = Color.FromRgb(240, 240, 240),
                IsVisible = false
            };

            // Container pour la photo avec bordure
            _photoContainer = new StackLayout
            {
                Children = { _photoImage },
                BackgroundColor = Colors.White,
                IsVisible = false
            };

            // Label de statut
            _statusLabel = new Label
            {
                Text = "📷 Prenez des photos de vos lieux favoris !",
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Colors.Gray,
                Padding = new Thickness(16, 8)
            };

            // Bouton prendre photo
            _takePhotoButton = new Button
            {
                Text = "📸 Prendre une photo",
                BackgroundColor = Colors.Green,
                TextColor = Colors.White,
                CornerRadius = 12,
                FontSize = 16,
                Padding = new Thickness(20, 12),
                Margin = new Thickness(0, 8)
            };
            _takePhotoButton.Clicked += OnTakePhotoClicked;

            // Bouton choisir photo
            _choosePhotoButton = new Button
            {
                Text = "🖼️ Choisir une photo",
                BackgroundColor = Colors.Blue,
                TextColor = Colors.White,
                CornerRadius = 12,
                FontSize = 16,
                Padding = new Thickness(20, 12),
                Margin = new Thickness(0, 4)
            };
            _choosePhotoButton.Clicked += OnChoosePhotoClicked;

            // Bouton partager
            _shareButton = new Button
            {
                Text = "📤 Partager la photo",
                BackgroundColor = Colors.Orange,
                TextColor = Colors.White,
                CornerRadius = 12,
                FontSize = 14,
                Padding = new Thickness(16, 8),
                Margin = new Thickness(0, 8),
                IsVisible = false
            };
            _shareButton.Clicked += OnSharePhotoClicked;

            // Layout principal
            var mainLayout = new StackLayout
            {
                Spacing = 16,
                Padding = 20,
                Children =
                {
                    new Label
                    {
                        Text = "📷 Photo des Lieux",
                        FontSize = 22,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center,
                        TextColor = Colors.DarkBlue
                    },
                    new Label
                    {
                        Text = "Immortalisez vos découvertes !",
                        FontSize = 14,
                        HorizontalTextAlignment = TextAlignment.Center,
                        TextColor = Colors.Gray,
                        FontAttributes = FontAttributes.Italic
                    },
                    _statusLabel,
                    _photoContainer,
                    new Frame
                    {
                        BackgroundColor = Color.FromRgb(245, 245, 255),
                        BorderColor = Colors.LightBlue,
                        CornerRadius = 12,
                        Padding = 16,
                        Content = new StackLayout
                        {
                            Children =
                            {
                                _takePhotoButton,
                                _choosePhotoButton,
                                _shareButton
                            }
                        }
                    },
                    new Frame
                    {
                        BackgroundColor = Color.FromRgb(255, 248, 220),
                        BorderColor = Colors.Orange,
                        CornerRadius = 8,
                        Padding = 12,
                        Content = new Label
                        {
                            Text = "💡 Astuce : Utilisez l'appareil photo pour documenter vos visites et créer votre carnet de voyage personnel !",
                            FontSize = 12,
                            TextColor = Colors.DarkOrange,
                            HorizontalTextAlignment = TextAlignment.Center
                        }
                    }
                }
            };

            Content = new Frame
            {
                Content = mainLayout,
                BackgroundColor = Colors.White,
                BorderColor = Colors.LightGray,
                CornerRadius = 16,
                Padding = 0,
                HasShadow = true
            };
        }

        private async void OnTakePhotoClicked(object? sender, EventArgs e)
        {
            try
            {
                _statusLabel.Text = "📸 Ouverture de l'appareil photo...";
                _statusLabel.TextColor = Colors.Blue;

                // Vérifier les permissions
                var cameraPermission = await Permissions.RequestAsync<Permissions.Camera>();
                if (cameraPermission != PermissionStatus.Granted)
                {
                    _statusLabel.Text = "❌ Permission appareil photo refusée";
                    _statusLabel.TextColor = Colors.Red;
                    return;
                }

                // Vérifier si l'appareil photo est disponible
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    _statusLabel.Text = "❌ Appareil photo non disponible";
                    _statusLabel.TextColor = Colors.Red;
                    return;
                }

                // Prendre la photo
                var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
                {
                    Title = "Photo TravelBuddy"
                });

                if (photo != null)
                {
                    // Sauvegarder la photo localement
                    _currentPhotoPath = await SavePhotoAsync(photo);
                    
                    // Afficher la photo
                    _photoImage.Source = ImageSource.FromFile(_currentPhotoPath);
                    _photoContainer.IsVisible = true;
                    _photoImage.IsVisible = true;
                    _shareButton.IsVisible = true;

                    _statusLabel.Text = $"✅ Photo capturée avec succès !";
                    _statusLabel.TextColor = Colors.Green;

                    // Animation de succès
                    await _photoContainer.ScaleTo(1.1, 200);
                    await _photoContainer.ScaleTo(1.0, 200);
                }
                else
                {
                    _statusLabel.Text = "❌ Capture annulée";
                    _statusLabel.TextColor = Colors.Orange;
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur: {ex.Message}";
                _statusLabel.TextColor = Colors.Red;
                Console.WriteLine($"❌ Erreur appareil photo: {ex.Message}");
            }
        }

        private async void OnChoosePhotoClicked(object? sender, EventArgs e)
        {
            try
            {
                _statusLabel.Text = "🖼️ Ouverture de la galerie...";
                _statusLabel.TextColor = Colors.Blue;

                var photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Choisir une photo"
                });

                if (photo != null)
                {
                    // Sauvegarder la photo localement
                    _currentPhotoPath = await SavePhotoAsync(photo);
                    
                    // Afficher la photo
                    _photoImage.Source = ImageSource.FromFile(_currentPhotoPath);
                    _photoContainer.IsVisible = true;
                    _photoImage.IsVisible = true;
                    _shareButton.IsVisible = true;

                    _statusLabel.Text = "✅ Photo sélectionnée avec succès !";
                    _statusLabel.TextColor = Colors.Green;

                    // Animation de succès
                    await _photoContainer.ScaleTo(1.05, 150);
                    await _photoContainer.ScaleTo(1.0, 150);
                }
                else
                {
                    _statusLabel.Text = "❌ Sélection annulée";
                    _statusLabel.TextColor = Colors.Orange;
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur: {ex.Message}";
                _statusLabel.TextColor = Colors.Red;
                Console.WriteLine($"❌ Erreur galerie: {ex.Message}");
            }
        }

        private async void OnSharePhotoClicked(object? sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentPhotoPath) || !File.Exists(_currentPhotoPath))
                {
                    _statusLabel.Text = "❌ Aucune photo à partager";
                    _statusLabel.TextColor = Colors.Red;
                    return;
                }

                _statusLabel.Text = "📤 Partage en cours...";
                _statusLabel.TextColor = Colors.Blue;

                // Créer la demande de partage
                var shareRequest = new ShareFileRequest
                {
                    Title = "Photo TravelBuddy",
                    File = new ShareFile(_currentPhotoPath)
                };

                // Partager la photo
                await Share.Default.RequestAsync(shareRequest);

                _statusLabel.Text = "✅ Photo partagée !";
                _statusLabel.TextColor = Colors.Green;
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erreur partage: {ex.Message}";
                _statusLabel.TextColor = Colors.Red;
                Console.WriteLine($"❌ Erreur partage: {ex.Message}");
            }
        }

        private async Task<string> SavePhotoAsync(FileResult photo)
        {
            try
            {
                // Créer un nom de fichier unique
                var fileName = $"travelbuddy_photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                var localPath = Path.Combine(FileSystem.CacheDirectory, fileName);

                // Copier la photo dans le stockage local
                using var sourceStream = await photo.OpenReadAsync();
                using var localFileStream = File.OpenWrite(localPath);
                await sourceStream.CopyToAsync(localFileStream);

                Console.WriteLine($"📸 Photo sauvegardée: {localPath}");
                return localPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur sauvegarde photo: {ex.Message}");
                throw;
            }
        }
    }
}