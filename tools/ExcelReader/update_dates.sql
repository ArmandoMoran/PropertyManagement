IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE Name = N'PurchaseDate' AND Object_ID = Object_ID(N'Properties'))
BEGIN
    ALTER TABLE Properties ADD PurchaseDate DATE NULL;
END
GO
UPDATE Properties SET PurchaseDate = '2006-09-14' WHERE PropertyId = 21;
UPDATE Properties SET PurchaseDate = '2007-03-22' WHERE PropertyId = 19;
UPDATE Properties SET PurchaseDate = '2015-01-15' WHERE PropertyId = 22;
UPDATE Properties SET PurchaseDate = '2015-09-24' WHERE PropertyId = 11;
UPDATE Properties SET PurchaseDate = '2016-02-23' WHERE PropertyId = 8;
UPDATE Properties SET PurchaseDate = '2016-03-17' WHERE PropertyId = 5;
UPDATE Properties SET PurchaseDate = '2017-12-27' WHERE PropertyId = 13;
UPDATE Properties SET PurchaseDate = '2018-01-19' WHERE PropertyId = 9;
UPDATE Properties SET PurchaseDate = '2018-01-31' WHERE PropertyId = 4;
UPDATE Properties SET PurchaseDate = '2018-02-05' WHERE PropertyId = 12;
UPDATE Properties SET PurchaseDate = '2018-09-14' WHERE PropertyId = 6;
UPDATE Properties SET PurchaseDate = '2019-07-31' WHERE PropertyId = 18;
UPDATE Properties SET PurchaseDate = '2020-01-30' WHERE PropertyId = 15;
UPDATE Properties SET PurchaseDate = '2020-08-31' WHERE PropertyId = 3;
UPDATE Properties SET PurchaseDate = '2020-11-20' WHERE PropertyId = 10;
UPDATE Properties SET PurchaseDate = '2021-01-22' WHERE PropertyId = 7;
UPDATE Properties SET PurchaseDate = '2021-03-12' WHERE PropertyId = 2;
UPDATE Properties SET PurchaseDate = '2021-03-26' WHERE PropertyId = 16;
UPDATE Properties SET PurchaseDate = '2021-10-14' WHERE PropertyId = 14;
UPDATE Properties SET PurchaseDate = '2021-11-19' WHERE PropertyId = 20;
UPDATE Properties SET PurchaseDate = '2021-12-30' WHERE PropertyId = 23;
UPDATE Properties SET PurchaseDate = '2022-04-29' WHERE PropertyId = 17;
UPDATE Properties SET PurchaseDate = '2022-07-02' WHERE PropertyId = 24;
