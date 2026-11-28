// Business Logic Layer
// Controls what the application should do and calls the data access layer
// when data access is needed.

using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Services
{
    public class ReservationService
    {
        private readonly IReservationRepository _repository;

        public ReservationService(IReservationRepository repository)
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

            // ReservationRepository creates the reservation and its fulfillment
            // task in the same SQL transaction. A reservation cannot silently
            // exist without a staff task.
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
            return allReservations
                .Where(r => r.DateReturned == null
                    && (r.ReservationStatus == "ReadyForPickup"
                        || r.ReservationStatus == "CheckedOut"))
                .ToList();
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

            if (!string.Equals(reservation.ReservationStatus, "CheckedOut", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only checked-out books can be returned.");

            return await _repository.ReturnReservationAsync(reservationId);
        }

        public async Task<bool> CheckoutBookAsync(int userId, int reservationId)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId must be greater than 0", nameof(userId));
            if (reservationId <= 0)
                throw new ArgumentException("ReservationId must be greater than 0", nameof(reservationId));

            return await _repository.CheckoutReservationAsync(userId, reservationId);
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

        
        public async Task<bool> CancelReservationAsync(int userId, int reservationId)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId must be greater than 0", nameof(userId));
            if (reservationId <= 0)
                throw new ArgumentException("ReservationId must be greater than 0", nameof(reservationId));

            return await _repository.CancelReservationAsync(userId, reservationId);
        }

        public async Task<int> ClearReservationHistoryAsync(int userId)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId must be greater than 0", nameof(userId));

            return await _repository.ClearReservationHistoryAsync(userId);
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
