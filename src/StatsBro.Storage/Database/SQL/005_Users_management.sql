CREATE TABLE Organizations (
	id				TEXT PRIMARY KEY,
	name			TEXT,
	createdAt		TIMESTAMP DEFAULT current_timestamp,
	updatedAt		TIMESTAMP DEFAULT current_timestamp
);

CREATE TABLE OrganizationUsers (
	organizationId	TEXT NOT NULL,
	userId			TEXT NOT NULL,
	role			TEXT NOT NULL,
	createdAt		TIMESTAMP DEFAULT current_timestamp,
	FOREIGN KEY (organizationId) REFERENCES Organizations(id),
	FOREIGN KEY (userId) REFERENCES Users(id)
);

ALTER TABLE Sites
	ADD organizationId TEXT
;


------------------------------------------------- LEGACY DATA
INSERT INTO Organizations(id, name)
	SELECT id, email
	FROM Users
;

UPDATE Sites
SET organizationId = us.userId
FROM (
	SELECT * 
	FROM UserSites 
	WHERE ID NOT IN 
		('FA75DE1C-AAAA-BBB1-12B7-0DAD49631111', '01D0994B-A54B-47A1-9526-40E98C33828A', '1A75DE1C-AAAA-BBBB-92B7-0DAD4963D122') 
	) us
WHERE Sites.id = us.siteId

;

INSERT INTO OrganizationUsers(organizationId, userId, role)
SELECT o.id, o.id, 'admin'
FROM Organizations o
;
