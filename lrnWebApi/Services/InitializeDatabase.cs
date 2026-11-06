using System;
using System.Diagnostics.Metrics;
using lrnWebApi.Database;
using lrnWebApi.Models;
using lrnWebApi.SettingCS;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace lrnWebApi.Services;

public class InitializeDatabase
{
    public static void CreateInitializeDatabase()
    {
        using var connection = new SqliteConnection(DatabaseSettings.ConnectionString);

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = @"
        PRAGMA foreign_keys = ON;

        /* =====================================================
           ROLES
           ===================================================== */

        CREATE TABLE IF NOT EXISTS Roles (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL UNIQUE
        );


        /* =====================================================
           USERS
           ===================================================== */

        CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,

            Username TEXT NOT NULL UNIQUE,

            Email TEXT NOT NULL UNIQUE,

            PasswordHash TEXT NOT NULL,

            RoleId INTEGER NOT NULL,

            CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,

            FOREIGN KEY (RoleId)
                REFERENCES Roles(Id)
        );

        CREATE TABLE IF NOT EXISTS UserApiKeys (
            UserId INTEGER PRIMARY KEY,
            EncryptedApiKey TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL,
            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS UserSessions (
            TokenHash TEXT PRIMARY KEY,
            UserId INTEGER NOT NULL,
            ExpiresAt TEXT NOT NULL,
            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
        );

        CREATE INDEX IF NOT EXISTS IX_UserSessions_ExpiresAt
        ON UserSessions(ExpiresAt);

        CREATE TABLE IF NOT EXISTS sentences (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            english TEXT,
            Renglish TEXT,
            turkish TEXT,
            grammar TEXT,
            topic TEXT,
            level TEXT,
            IsUsed INTEGER NOT NULL DEFAULT 0 CHECK (IsUsed IN (0, 1))
        );


        /* =====================================================
           RANKS
           ===================================================== */

        CREATE TABLE IF NOT EXISTS Ranks (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,

            Name TEXT NOT NULL UNIQUE,

            MinXP INTEGER NOT NULL,

            MaxXP INTEGER NOT NULL
        );


        /* =====================================================
           USER STATS
           ===================================================== */

        CREATE TABLE IF NOT EXISTS UserStats (
            UserId INTEGER PRIMARY KEY,

            XP INTEGER NOT NULL DEFAULT 0,

            Level INTEGER NOT NULL DEFAULT 1,

            RankId INTEGER NOT NULL,

            UpdatedAt TEXT NOT NULL,

            FOREIGN KEY (UserId)
                REFERENCES Users(Id)
                ON DELETE CASCADE,

            FOREIGN KEY (RankId)
                REFERENCES Ranks(Id)
        );


        /* =====================================================
           LANGUAGE LEVELS
           ===================================================== */

        CREATE TABLE IF NOT EXISTS LanguageLevels (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,

            Code TEXT NOT NULL UNIQUE,

            Name TEXT NOT NULL
        );


        /* =====================================================
           COURSES
           ===================================================== */

        CREATE TABLE IF NOT EXISTS Courses (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,

            Name TEXT NOT NULL,

            Description TEXT,

            LanguageLevelId INTEGER NOT NULL,

            OrderNo INTEGER NOT NULL DEFAULT 0,

            IsActive INTEGER NOT NULL DEFAULT 1,

            FOREIGN KEY (LanguageLevelId)
                REFERENCES LanguageLevels(Id)
        );


        /* =====================================================
           UNITS
           ===================================================== */

        CREATE TABLE IF NOT EXISTS Units (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,

            CourseId INTEGER NOT NULL,

            Name TEXT NOT NULL,

            Description TEXT,

            OrderNo INTEGER NOT NULL DEFAULT 0,

            FOREIGN KEY (CourseId)
                REFERENCES Courses(Id)
                ON DELETE CASCADE
        );


        /* =====================================================
           USER COURSE PROGRESS
           ===================================================== */

        CREATE TABLE IF NOT EXISTS UserCourseProgress (
            UserId INTEGER NOT NULL,

            CourseId INTEGER NOT NULL,

            Progress INTEGER NOT NULL DEFAULT 0,

            StartedAt TEXT NOT NULL,

            CompletedAt TEXT,

            PRIMARY KEY (UserId, CourseId),

            FOREIGN KEY (UserId)
                REFERENCES Users(Id)
                ON DELETE CASCADE,

            FOREIGN KEY (CourseId)
                REFERENCES Courses(Id)
                ON DELETE CASCADE
        );


        /* =====================================================
           USER UNIT PROGRESS
           ===================================================== */

        CREATE TABLE IF NOT EXISTS UserUnitProgress (
            UserId INTEGER NOT NULL,

            UnitId INTEGER NOT NULL,

            Progress INTEGER NOT NULL DEFAULT 0,

            Completed INTEGER NOT NULL DEFAULT 0,

            StartedAt TEXT NOT NULL,

            CompletedAt TEXT,

            PRIMARY KEY (UserId, UnitId),

            FOREIGN KEY (UserId)
                REFERENCES Users(Id)
                ON DELETE CASCADE,

            FOREIGN KEY (UnitId)
                REFERENCES Units(Id)
                ON DELETE CASCADE
        );


        /* =====================================================
           INDEXES
           ===================================================== */

        CREATE INDEX IF NOT EXISTS IX_Users_Email
        ON Users(Email);

        CREATE INDEX IF NOT EXISTS IX_Users_Username
        ON Users(Username);

        CREATE INDEX IF NOT EXISTS IX_Units_CourseId
        ON Units(CourseId);

        CREATE INDEX IF NOT EXISTS IX_UserCourseProgress_UserId
        ON UserCourseProgress(UserId);

        CREATE INDEX IF NOT EXISTS IX_UserUnitProgress_UserId
        ON UserUnitProgress(UserId);


        /* =====================================================
           DEFAULT ROLES
           ===================================================== */

        INSERT OR IGNORE INTO Roles (Name)
        VALUES
            ('User'),
            ('Admin');


        /* =====================================================
           DEFAULT RANKS
           ===================================================== */

        INSERT OR IGNORE INTO Ranks (Name, MinXP, MaxXP)
        VALUES
            ('Bronze',   0,     999),
            ('Silver',   1000,  2499),
            ('Gold',     2500,  4999),
            ('Platinum', 5000,  9999),
            ('Diamond',  10000, 19999);


        /* =====================================================
           LANGUAGE LEVELS
           ===================================================== */

        INSERT OR IGNORE INTO LanguageLevels (Code, Name)
        VALUES
            ('A1', 'Beginner'),
            ('A2', 'Elementary'),
            ('B1', 'Intermediate'),
            ('B2', 'Upper-Intermediate'),
            ('C1', 'Advanced'),
            ('C2', 'Proficient');
        ";

        command.ExecuteNonQuery();

        EnsureSentenceUsageColumn(connection);
    }

    private static void EnsureSentenceUsageColumn(SqliteConnection connection)
    {
        using var inspect = connection.CreateCommand();
        inspect.CommandText = "PRAGMA table_info(sentences);";
        using var reader = inspect.ExecuteReader();

        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            columns.Add(reader.GetString(1));
        }
        reader.Close();

        if (columns.Contains("IsUsed"))
        {
            return;
        }

        using var migration = connection.CreateCommand();
        migration.CommandText = "ALTER TABLE sentences ADD COLUMN IsUsed INTEGER NOT NULL DEFAULT 0 CHECK (IsUsed IN (0, 1));";
        migration.ExecuteNonQuery();

        // Older local databases used a Turkish dotless-i column name.
        if (columns.Contains("ısUsed"))
        {
            using var copyLegacyValues = connection.CreateCommand();
            copyLegacyValues.CommandText = "UPDATE sentences SET IsUsed = [ısUsed];";
            copyLegacyValues.ExecuteNonQuery();
        }
    }
}
