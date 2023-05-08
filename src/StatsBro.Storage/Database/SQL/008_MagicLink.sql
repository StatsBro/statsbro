
CREATE TABLE MagicLinks (
	id				TEXT PRIMARY KEY,
	validTo			TIMESTAMP DEFAULT current_timestamp,
	userId			TEXT,
	origin			TEXT NOT NULL DEFAULT 'Login',
	createdAt		TIMESTAMP DEFAULT current_timestamp,
	FOREIGN KEY (userId) REFERENCES Users(id)
);
