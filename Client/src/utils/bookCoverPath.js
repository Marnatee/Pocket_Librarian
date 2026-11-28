// Static catalogue covers are keyed by the BookID returned by the API.
export function getBookCoverPath(bookId) {
    const numericBookId = Number(bookId);

    return Number.isInteger(numericBookId) && numericBookId > 0
        ? `/featured-books/book-${numericBookId}.jpg`
        : "";
}
