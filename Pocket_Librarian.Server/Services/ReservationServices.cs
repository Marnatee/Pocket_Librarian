// Business Logic Layer
// Controls what the application should do and calls the data access layer
// when data access is needed.

using Pocket_Librarian.Server.Models;
using Pocket_Librarian.Server.Repositories;

namespace Pocket_Librarian.Server.Services
{
    public class ReservationService
    {
        private readonly ReservationRepository _repository;

        public ReservationService(ReservationRepository repository)
        {
            _repository = repository;
        }

        //Creates a new reservation for a user checking out a book
        public async Task<int> CreateReservationAsync(int userId, int bookId)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId must be greater than 0", nameof(userId));

            if (bookId <= 0)
                throw new ArgumentException("BookId must be greater than 0", nameof(bookId));

            var reservation = new Reservation
            {
                UserId = userId,
                BookId = bookId,
                DateReserved = DateTime.UtcNow,
                DateReturned = null
            };

            return await _repository.CreateReservationAsync(reservation);
        }

        
        public async Task<Reservation?> GetReservationByIdAsync(int reservationId)
        {
            if (reservationId <= 0)
                throw new ArgumentException("ReservationId must be greater than 0", nameof(reservationId));

            return await _repository.GetReservationByIdAsync(reservationId);
        }

        
        public async Task<List<Reservation>> GetUserReservationHistoryAsync(int userId)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId must be greater than 0", nameof(userId));

            return await _repository.GetReservationsByUserIdAsync(userId);
        }

        
        public async Task<List<Reservation>> GetUserActiveReservationsAsync(int userId)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId must be greater than 0", nameof(userId));

            var allReservations = await _repository.GetReservationsByUserIdAsync(userId);
            return allReservations.Where(r => r.DateReturned == null).ToList();
        }

        
        public async Task<List<Reservation>> GetBookReservationHistoryAsync(int bookId)
        {
            if (bookId <= 0)
                throw new ArgumentException("BookId must be greater than 0", nameof(bookId));

            return await _repository.GetReservationsByBookIdAsync(bookId);
        }

        
        public async Task<List<Reservation>> GetAllActiveReservationsAsync()
        {
            return await _repository.GetActiveReservationsAsync();
        }

        
        public async Task<bool> ReturnBookAsync(int reservationId)
        {
            if (reservationId <= 0)
                throw new ArgumentException("ReservationId must be greater than 0", nameof(reservationId));

            var reservation = await _repository.GetReservationByIdAsync(reservationId);

            if (reservation == null)
                throw new InvalidOperationException($"Reservation with ID {reservationId} not found");

            if (reservation.DateReturned != null)
                throw new InvalidOperationException($"Reservation {reservationId} has already been returned");

            return await _repository.ReturnReservationAsync(reservationId);
        }

        
        public async Task<bool> UpdateReservationAsync(Reservation reservation)
        {
            if (reservation == null)
                throw new ArgumentNullException(nameof(reservation));

            if (reservation.ReservationId <= 0)
                throw new ArgumentException("ReservationId must be greater than 0", nameof(reservation.ReservationId));

            if (reservation.UserId <= 0)
                throw new ArgumentException("UserId must be greater than 0", nameof(reservation.UserId));

            if (reservation.BookId <= 0)
                throw new ArgumentException("BookId must be greater than 0", nameof(reservation.BookId));

            return await _repository.UpdateReservationAsync(reservation);
        }

        
        public async Task<bool> DeleteReservationAsync(int reservationId)
        {
            if (reservationId <= 0)
                throw new ArgumentException("ReservationId must be greater than 0", nameof(reservationId));

            return await _repository.DeleteReservationAsync(reservationId);
        }

        
        public TimeSpan GetReservationDuration(Reservation reservation)
        {
            if (reservation == null)
                throw new ArgumentNullException(nameof(reservation));

            DateTime endDate = reservation.DateReturned ?? DateTime.UtcNow;
            return endDate - reservation.DateReserved;
        }

        
        public async Task<bool> IsBookAvailableAsync(int bookId)
        {
            if (bookId <= 0)
                throw new ArgumentException("BookId must be greater than 0", nameof(bookId));

            return await _repository.IsBookAvailableAsync(bookId);
        }
    }
}
