using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Interfaces;

public interface IReservationRepository
{
    Task<int> CreateReservationAsync(Reservation reservation);

    Task<Reservation?> GetReservationByIdAsync(int reservationId);

    Task<List<Reservation>> GetReservationsByUserIdAsync(int userId);

    Task<List<Reservation>> GetReservationsByBookIdAsync(int bookId);

    Task<List<Reservation>> GetActiveReservationsAsync();

    Task<bool> UpdateReservationAsync(Reservation reservation);

    Task<bool> ReturnReservationAsync(int reservationId);

    Task<bool> IsBookAvailableAsync(int bookId);

    Task<bool> DeleteReservationAsync(int reservationId);
}
