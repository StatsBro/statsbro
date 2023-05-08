CREATE TABLE Referrals (
	id				TEXT PRIMARY KEY,
	organizationId	TEXT NOT NULL,
	referralKey		INT NOT NULL,
	cid				TEXT,
	createdAt		TIMESTAMP DEFAULT current_timestamp,
	FOREIGN KEY (organizationId) REFERENCES Organizations(id)
);


