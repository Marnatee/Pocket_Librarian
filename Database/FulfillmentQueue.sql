-- Fulfillment Queue Schema
-- Creates the FulfillmentTasks table for managing reserved books awaiting staff locating and member pickup.
-- Run this script after the core database tables (Users, Reservations, Books) exist.

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FulfillmentTasks')
BEGIN
	CREATE TABLE dbo.FulfillmentTasks
	(
		FulfillmentTaskID INT NOT NULL IDENTITY (1, 1) PRIMARY KEY,
		ReservationID INT NOT NULL,
		UserID INT NOT NULL,
		BookID INT NOT NULL,
		AssignedStaffID INT NULL,
		BookTitle NVARCHAR(300) NOT NULL,
		MemberName NVARCHAR(300) NOT NULL,
		Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
		DateCreated DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
		DateAssigned DATETIME2 NULL,
		DateFulfilled DATETIME2 NULL,

		CONSTRAINT FK_FulfillmentTasks_Reservations 
			FOREIGN KEY (ReservationID) REFERENCES dbo.Reservations (ReservationID),
		CONSTRAINT FK_FulfillmentTasks_Users_Member 
			FOREIGN KEY (UserID) REFERENCES dbo.Users (UserID),
		CONSTRAINT FK_FulfillmentTasks_Books 
			FOREIGN KEY (BookID) REFERENCES dbo.Books (BookID),
		CONSTRAINT FK_FulfillmentTasks_Users_Staff 
			FOREIGN KEY (AssignedStaffID) REFERENCES dbo.Users (UserID),

		CONSTRAINT CK_FulfillmentTasks_Status 
			CHECK (Status IN ('Pending', 'InProgress', 'Completed'))
	);

	-- Create indexes for common queries
	CREATE NONCLUSTERED INDEX IX_FulfillmentTasks_Status_DateCreated 
		ON dbo.FulfillmentTasks (Status, DateCreated);

	CREATE NONCLUSTERED INDEX IX_FulfillmentTasks_AssignedStaffID_Status 
		ON dbo.FulfillmentTasks (AssignedStaffID, Status);

	CREATE NONCLUSTERED INDEX IX_FulfillmentTasks_UserID 
		ON dbo.FulfillmentTasks (UserID);

	CREATE NONCLUSTERED INDEX IX_FulfillmentTasks_DateFulfilled 
		ON dbo.FulfillmentTasks (DateFulfilled) 
		WHERE DateFulfilled IS NOT NULL;

	PRINT 'FulfillmentTasks table created successfully.';
END
ELSE
BEGIN
	PRINT 'FulfillmentTasks table already exists.';
END
