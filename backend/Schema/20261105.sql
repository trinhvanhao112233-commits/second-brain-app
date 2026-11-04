CREATE TABLE IF NOT EXISTS ReminderDeliveries (
          Id varchar(64) NOT NULL PRIMARY KEY, UserId varchar(36) NOT NULL,
          SubscriptionId varchar(36) NOT NULL, Sent tinyint(1) NOT NULL,
          AttemptedAt datetime(6) NOT NULL
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Users` (
            `Id` char(36) COLLATE utf8mb4_general_ci NOT NULL,
            `Username` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
            `FullName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
            `PasswordHash` longtext CHARACTER SET utf8mb4 NOT NULL,
            `CreatedAt` datetime(6) NOT NULL,
            PRIMARY KEY (`Id`),
            UNIQUE KEY `IX_Users_Username` (`Username`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `DebtRecords` (
            `Id` char(36) COLLATE utf8mb4_general_ci NOT NULL,
            `UserId` char(36) COLLATE utf8mb4_general_ci NULL,
            `Title` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
            `RecordType` varchar(30) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'DEBT',
            `TotalAmount` decimal(18,2) NOT NULL DEFAULT 0,
            `PaidAmount` decimal(18,2) NOT NULL DEFAULT 0,
            `BreakdownNote` varchar(500) CHARACTER SET utf8mb4 NULL,
            `DueDate` datetime(6) NOT NULL,
            `PeriodLabel` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT '1-10',
            `PriorityLevel` int NOT NULL DEFAULT 2,
            `IsCompleted` tinyint(1) NOT NULL DEFAULT 0,
            `CategoryTag` varchar(100) CHARACTER SET utf8mb4 NULL,
            `CreatedAt` datetime(6) NOT NULL,
            PRIMARY KEY (`Id`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `PushSubscriptions` (
            `Id` char(36) COLLATE utf8mb4_general_ci NOT NULL,
            `UserId` char(36) COLLATE utf8mb4_general_ci NULL,
            `Endpoint` text COLLATE utf8mb4_general_ci NOT NULL,
            `P256dh` varchar(255) COLLATE utf8mb4_general_ci NOT NULL,
            `Auth` varchar(255) COLLATE utf8mb4_general_ci NOT NULL,
            `CreatedAt` datetime(6) NOT NULL,
            PRIMARY KEY (`Id`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

ALTER TABLE Transactions ADD COLUMN TransferId varchar(36) NULL;

ALTER TABLE Users ADD COLUMN NotificationSettings longtext NULL;

ALTER TABLE `Wallets` ADD COLUMN `UserId` char(36) COLLATE utf8mb4_general_ci NULL;

ALTER TABLE `Wallets` ADD COLUMN `IsMaster` tinyint(1) NOT NULL DEFAULT 0;

ALTER TABLE `Events` ADD COLUMN `UserId` char(36) COLLATE utf8mb4_general_ci NULL;

ALTER TABLE `Events` ADD COLUMN `IsReminderSent` tinyint(1) NOT NULL DEFAULT 0;

ALTER TABLE `Tasks` ADD COLUMN `UserId` char(36) COLLATE utf8mb4_general_ci NULL;

ALTER TABLE `Notes` ADD COLUMN `UserId` char(36) COLLATE utf8mb4_general_ci NULL;

ALTER TABLE Transactions ADD COLUMN DebtRecordId varchar(36) NULL;
