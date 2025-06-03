using System.Threading.Tasks;
using System.Collections.Generic;
using MyApp.Models;

namespace MyApp.Services
{
    public interface IPlaceService
    {
        /// <summary>
        /// Recherche des lieux touristiques à proximité d'une position géographique
        /// </summary>
        /// <param name="latitude">Latitude de la position</param>
        /// <param name="longitude">Longitude de la position</param>
        /// <param name="query">Terme de recherche optionnel</param>
        /// <param name="radius">Rayon de recherche en mètres (par défaut 1000m)</param>
        /// <param name="limit">Nombre maximum de résultats (par défaut 20)</param>
        /// <returns>Liste des lieux trouvés</returns>
        Task<List<Place>> GetNearbyPlacesAsync(double latitude, double longitude, string query = null, int radius = 1000, int limit = 20);
        
        /// <summary>
        /// Obtient les détails d'un lieu spécifique
        /// </summary>
        /// <param name="placeId">Identifiant du lieu</param>
        /// <returns>Détails du lieu</returns>
        Task<Place> GetPlaceDetailsAsync(string placeId);
    }
}