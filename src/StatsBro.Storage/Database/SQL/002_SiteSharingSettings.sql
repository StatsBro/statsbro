CREATE TABLE SiteSharingSettings (
	siteId		TEXT PRIMARY KEY,
	linkShareId TEXT,
	FOREIGN KEY (siteId) REFERENCES Sites(id)
);

CREATE UNIQUE INDEX IDX_UQ_SiteSharingSettings_LinkShareId ON SiteSharingSettings(linkShareId)
;