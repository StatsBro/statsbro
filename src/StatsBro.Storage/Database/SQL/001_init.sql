CREATE TABLE Sites (
	id					TEXT PRIMARY KEY,
	domain				TEXT NOT NULL,
	persistQueryParams	TEXT,
	ignoreIPs			TEXT,
	createdAt			TIMESTAMP DEFAULT current_timestamp,
	updatedAt			TIMESTAMP DEFAULT current_timestamp
);

CREATE TABLE Users (
	id				TEXT PRIMARY KEY,
	email			TEXT NOT NULL,
	passwordHash	TEXT,
	passwordSalt	TEXT,
	registeredAt	TIMESTAMP DEFAULT current_timestamp,
	updatedAt		TIMESTAMP DEFAULT current_timestamp
);

CREATE TABLE UserSites (
	id			TEXT PRIMARY KEY,
	userId		TEXT NOT NULL,
	siteId		TEXT NOT NULL,
	createdAt	TIMESTAMP DEFAULT current_timestamp,
	updatedAt		TIMESTAMP DEFAULT current_timestamp,
	FOREIGN KEY (userId) REFERENCES Users(id),
	FOREIGN KEY (siteId) REFERENCES Sites(id)
);



-- date time (UTC ticks):
-- 637987035286478377


INSERT INTO Sites
	VALUES('BB75DE1C-CCCC-2222-92B7-0DAD4963D922', 'statsbro.io', 'ref;source;utm_campaign;utm_medium;utm_source;utm_content;utm_term', NULL, datetime('now','utc'), datetime('now','utc'))
;

INSERT INTO Sites
	VALUES('2275DE1C-CCCC-2222-92B7-0DAD49612988', 'localhost', 'ref;source;utm_campaign;utm_medium;utm_source;utm_content;utm_term', '', datetime('now','utc'), datetime('now','utc'))
;

INSERT INTO Users
	VALUES('FA75DE1C-AAAA-BBB5-92B7-0DAD4963D9F6', 'demo@statsbro.io', 'TcZDgPazCjoq7dPC7ykO6yk6NwnvElSOpEU/Ybe2zfo8t6nsJGmLwYlURDPQmwyFPXP9eEqIVzmlfJaF5Dsecw==', 'SxBrPe8qd8NDgwQatZI2uGpGLzMBJqxJjLF32YuC9VHNjGgEpuYHWQA6zBGI8QMf05maXqmO3r91N4RoCiU4eg==', datetime('now','utc'), datetime('now','utc'))
;

INSERT INTO Users
	VALUES('FA75DE1C-1234-BBB5-92B7-0DAD4963D9F6', 'example@example.com', 'TcZDgPazCjoq7dPC7ykO6yk6NwnvElSOpEU/Ybe2zfo8t6nsJGmLwYlURDPQmwyFPXP9eEqIVzmlfJaF5Dsecw==', 'SxBrPe8qd8NDgwQatZI2uGpGLzMBJqxJjLF32YuC9VHNjGgEpuYHWQA6zBGI8QMf05maXqmO3r91N4RoCiU4eg==', datetime('now','utc'), datetime('now','utc'))
;


INSERT INTO UserSites
	VALUES('FA75DE1C-AAAA-BBB1-12B7-0DAD49631111', 'FA75DE1C-AAAA-BBB5-92B7-0DAD4963D9F6','BB75DE1C-CCCC-2222-92B7-0DAD4963D922', datetime('now','utc'), datetime('now','utc'))
;
INSERT INTO UserSites
	VALUES('1A75DE1C-AAAA-BBBB-92B7-0DAD4963D111', 'FA75DE1C-1234-BBB5-92B7-0DAD4963D9F6', '2275DE1C-CCCC-2222-92B7-0DAD49612988', datetime('now','utc'), datetime('now','utc'))
;