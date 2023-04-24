
CREATE TABLE Payments (
	id				TEXT PRIMARY KEY,
	idFromProvider	TEXT,
	provider		TEXT NOT NULL DEFAULT 'PayU',
	organizationId	TEXT NOT NULL,
	userId			TEXT,
	status			TEXT NOT NULL DEFAULT 'New',
	source			TEXT NOT NULL DEFAULT 'Manual', --manual, recurring
	createdAt		TIMESTAMP DEFAULT current_timestamp,
	updatedAt		TIMESTAMP DEFAULT current_timestamp,
	FOREIGN KEY (organizationId) REFERENCES Organizations(id),
	FOREIGN KEY (userId) REFERENCES Users(id)
);

CREATE TABLE PaymentTransactions (
	id					TEXT PRIMARY KEY,
	organizationId		TEXT NOT NULL,
	paymentId			TEXT NOT NULL,
	amountNet			NUMERIC NOT NULL,
	amountGross			NUMERIC NOT NULL,
	vatValue			NUMERIC NOT NULL,
	vatAmount			NUMERIC NOT NULL,
	currency			TEXT NOT NULL,
	subscriptionType	TEXT NOT NULL,
	subscriptionContinueTo TIMESTAMP, 
	status				TEXT NOT NULL DEFAULT 'New', -- New, Paid, Refunded, Failed, Cancelled
	createdAt			TIMESTAMP DEFAULT current_timestamp,
	updatedAt			TIMESTAMP DEFAULT current_timestamp,
	FOREIGN KEY (organizationId) REFERENCES Organizations(id),
	FOREIGN KEY (paymentId) REFERENCES Payments(id)
);

CREATE TABLE CreditCardTokens (
	id				TEXT PRIMARY KEY,
	organizationId	TEXT NOT NULL,
	userId			TEXT NOT NULL,
	provider		TEXT NOT NULL,
	token			TEXT NOT NULL,
	cardNumberMasked TEXT NOT NULL,
	cardExpirationMonth NUMBER NOT NULL,
	cardExpirationYear NUMBER NOT NULL,
	createdAt		TIMESTAMP DEFAULT current_timestamp,
	updatedAt		TIMESTAMP DEFAULT current_timestamp,
	FOREIGN KEY (organizationId) REFERENCES Organizations(id),
	FOREIGN KEY (userId) REFERENCES Users(id)
);

CREATE TABLE OrganizationAddresses (
	organizationId	TEXT NOT NULL,
	name			TEXT NOT NULL,
	addressLine1	TEXT,
	postalCode		TEXT,
	city			TEXT,
	nip				TEXT,
	createdAt		TIMESTAMP DEFAULT current_timestamp,
	FOREIGN KEY (organizationId) REFERENCES Organizations(id)
);

CREATE INDEX IDX_OrganizationAddresses_organizationId ON OrganizationAddresses(organizationId)
;

ALTER TABLE Organizations 
	ADD subscriptionType TEXT
;

ALTER TABLE Organizations 
	ADD subscriptionValidTo DATE
;

ALTER TABLE Organizations 
	ADD isSubscriptionCancelled BIT DEFAULT 0
;


UPDATE Organizations
	SET
		subscriptionType = 'trial'
		,subscriptionValidTo = date(current_timestamp, '+6 days')
;


CREATE UNIQUE INDEX IDX_UQ_Payments_idFromProvider ON Payments(idFromProvider)
;


DROP TABLE UserSites;

