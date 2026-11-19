import { useCallback, useEffect, useState } from "react";

async function getApiMessage(response, fallback) {
    const data = await response.json().catch(() => ({}));
    return data.message || fallback;
}

function MemberNotifications() {
    const [notifications, setNotifications] = useState([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState("");

    const loadNotifications = useCallback(async () => {
        setIsLoading(true);
        setError("");

        try {
            const response = await fetch("/api/notifications", {
                credentials: "include",
            });

            if (!response.ok) {
                throw new Error(await getApiMessage(
                    response,
                    "Notifications could not be loaded."
                ));
            }

            setNotifications(await response.json());
        } catch (loadError) {
            setError(loadError.message);
        } finally {
            setIsLoading(false);
        }
    }, []);

    useEffect(() => {
        const initialLoad = window.setTimeout(() => {
            void loadNotifications();
        }, 0);

        return () => window.clearTimeout(initialLoad);
    }, [loadNotifications]);

    async function markAsRead(notificationId) {
        setError("");

        try {
            const response = await fetch(
                `/api/notifications/${notificationId}/read`,
                {
                    method: "PUT",
                    credentials: "include",
                }
            );

            if (!response.ok) {
                throw new Error(await getApiMessage(
                    response,
                    "The notification could not be marked as read."
                ));
            }

            setNotifications((currentNotifications) =>
                currentNotifications.map((notification) =>
                    notification.notificationId === notificationId
                        ? { ...notification, isRead: true }
                        : notification
                )
            );
        } catch (markError) {
            setError(markError.message);
        }
    }

    const unreadCount = notifications.filter(
        (notification) => !notification.isRead
    ).length;

    return (
        <section className="member-feature-card" aria-labelledby="notifications-heading">
            <div className="member-feature-heading">
                <div>
                    <p className="page-eyebrow">Account activity</p>
                    <h2 id="notifications-heading">Notifications</h2>
                </div>
                <button
                    type="button"
                    className="member-secondary-button"
                    onClick={() => void loadNotifications()}
                    disabled={isLoading}
                >
                    Refresh
                </button>
            </div>

            <p className="member-muted-text">
                {unreadCount === 0
                    ? "No unread notifications."
                    : `${unreadCount} unread notification${unreadCount === 1 ? "" : "s"}.`}
            </p>

            {error && <p className="member-error" role="alert">{error}</p>}
            {isLoading && <p className="member-muted-text">Loading notifications...</p>}

            {!isLoading && notifications.length === 0 && (
                <p className="member-muted-text">Your notification inbox is empty.</p>
            )}

            <div className="notification-list">
                {notifications.map((notification) => (
                    <article
                        className={`notification-card${notification.isRead ? " is-read" : ""}`}
                        key={notification.notificationId}
                    >
                        <div>
                            <h3>{notification.title}</h3>
                            <p>{notification.messageText}</p>
                            <time dateTime={notification.createdDate}>
                                {new Date(notification.createdDate).toLocaleString()}
                            </time>
                        </div>
                        {!notification.isRead && (
                            <button
                                type="button"
                                className="member-secondary-button"
                                onClick={() => void markAsRead(notification.notificationId)}
                            >
                                Mark read
                            </button>
                        )}
                    </article>
                ))}
            </div>
        </section>
    );
}

export default MemberNotifications;
