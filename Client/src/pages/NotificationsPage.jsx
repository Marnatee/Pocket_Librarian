// Presentation layer / UI
// Full notification history for the signed-in member.

import { useCallback, useEffect, useState } from "react";

async function getApiMessage(response, fallback) {
    const data = await response.json().catch(() => ({}));
    return data.message || fallback;
}

function NotificationsPage({ onBack }) {
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

    async function deleteNotification(notificationId) {
        if (!window.confirm("Delete this notification permanently?")) {
            return;
        }

        setError("");

        try {
            const response = await fetch(
                `/api/notifications/${notificationId}`,
                {
                    method: "DELETE",
                    credentials: "include",
                }
            );

            if (!response.ok) {
                throw new Error(await getApiMessage(
                    response,
                    "The notification could not be deleted."
                ));
            }

            setNotifications((currentNotifications) =>
                currentNotifications.filter(
                    (notification) => notification.notificationId !== notificationId
                )
            );
        } catch (deleteError) {
            setError(deleteError.message);
        }
    }

    return (
        <section className="page-view notifications-page" aria-labelledby="notifications-page-heading">
            <div className="page-introduction">
                <p className="page-eyebrow">Account activity</p>
                <h1 id="notifications-page-heading">All notifications</h1>
                <p>Read, review, and remove notifications saved to your account.</p>
            </div>

            <button
                className="btn-back-to-features"
                type="button"
                onClick={onBack}
            >
                ← Back to Member Page
            </button>

            <section className="member-feature-card" aria-labelledby="notification-history-heading">
                <div className="member-feature-heading">
                    <div>
                        <p className="page-eyebrow">Saved history</p>
                        <h2 id="notification-history-heading">Notification history</h2>
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

                {error && <p className="member-error" role="alert">{error}</p>}
                {isLoading && <p className="member-muted-text">Loading notifications...</p>}

                {!isLoading && notifications.length === 0 && (
                    <p className="member-muted-text">There are no saved notifications.</p>
                )}

                <div className="notification-list">
                    {notifications.map((notification) => (
                        <article
                            className={`notification-card${notification.isRead ? " is-read" : ""}`}
                            key={notification.notificationId}
                        >
                            <div>
                                <div className="notification-card-heading">
                                    <h3>{notification.title}</h3>
                                    <span className="notification-status">
                                        {notification.isRead ? "Read" : "Unread"}
                                    </span>
                                </div>
                                <p>{notification.messageText}</p>
                                <time dateTime={notification.createdDate}>
                                    {new Date(notification.createdDate).toLocaleString()}
                                </time>
                            </div>
                            <div className="notification-actions">
                                {!notification.isRead && (
                                    <button
                                        type="button"
                                        className="member-secondary-button"
                                        onClick={() => void markAsRead(notification.notificationId)}
                                    >
                                        Mark read
                                    </button>
                                )}
                                <button
                                    type="button"
                                    className="member-danger-button"
                                    onClick={() => void deleteNotification(notification.notificationId)}
                                >
                                    Delete
                                </button>
                            </div>
                        </article>
                    ))}
                </div>
            </section>
        </section>
    );
}

export default NotificationsPage;
