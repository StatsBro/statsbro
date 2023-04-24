CREATE TABLE SiteApiSettings (
	siteId		TEXT PRIMARY KEY,
	apiKey		TEXT,
	FOREIGN KEY (siteId) REFERENCES Sites(id)
);

CREATE UNIQUE INDEX IDX_UQ_SiteApiSettings_ApiKey ON SiteApiSettings(apiKey)
;