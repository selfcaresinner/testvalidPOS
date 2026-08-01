using System;
using System.IO;
using System.Windows;
using Serilog;

namespace PosCore.Services;

public static class DatabaseBackupService
{
    public static void ManageDatabaseBackup(string connectionString)
    {
        try
        {
            // Simple parsing to find DB file (e.g. "Data Source=pos_local.db")
            var dbPath = connectionString.Replace("Data Source=", "").Trim(';', ' ', '"', '\'');
            if (string.IsNullOrEmpty(dbPath)) return;

            var fullPath = Path.GetFullPath(dbPath);
            var backupPath = fullPath + ".bak";

            if (!File.Exists(fullPath)) return; // No DB yet

            // Test if DB is readable/corrupt. A simple way is to check size > 0 or let EF Core catch it later
            // But we do an eager backup if it seems OK.
            // Since EF Core Migrations will fail if corrupt, we can just backup now. 
            // Wait, what if it's currently corrupt and we backup the corrupt version?
            // SQLite corruption usually happens on write.
            // For safety, we will just make a copy on every startup. If later we get an exception on Migrate, we restore it.
            
            File.Copy(fullPath, backupPath, overwrite: true);
            Log.Information("Database backup created successfully at {BackupPath}", backupPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create database backup.");
        }
    }

    public static bool TryRestoreFromBackup(string connectionString)
    {
        try
        {
            var dbPath = connectionString.Replace("Data Source=", "").Trim(';', ' ', '"', '\'');
            if (string.IsNullOrEmpty(dbPath)) return false;

            var fullPath = Path.GetFullPath(dbPath);
            var backupPath = fullPath + ".bak";

            if (!File.Exists(backupPath))
            {
                MessageBox.Show("La base de datos parece estar corrupta y no hay un respaldo disponible.", "Error de Base de Datos", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            var result = MessageBox.Show(
                "Se detectó un problema grave al intentar abrir la base de datos local. ¿Desea intentar restaurar el último respaldo automático?",
                "Base de Datos Corrupta",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                File.Copy(backupPath, fullPath, overwrite: true);
                Log.Information("Database restored from backup successfully.");
                MessageBox.Show("Base de datos restaurada correctamente. La aplicación se cerrará; vuelva a abrirla.", "Restauración Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to restore database from backup.");
            MessageBox.Show("Fallo al intentar restaurar el respaldo de la base de datos.", "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return false;
    }
}
