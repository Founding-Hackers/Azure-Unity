﻿using Microsoft.WindowsAzure.MobileServices;
using Microsoft.WindowsAzure.MobileServices.SQLiteStore;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LightBuzz
{
    /// <summary>
    /// Support local SQLite database.
    /// </summary>
    public class LocalStore
    {
        private static readonly string LocalDatabaseName = "database.db";

        private static MobileServiceSQLiteStore _localStore;

        private static MobileServiceClient _azureClient;

        private static string LocalDatabasePath
        {
            get
            {
                return Path.Combine(Application.persistentDataPath, LocalDatabaseName);
            }
        }

        private static string LocalDatabaseConnectionString
        {
            get
            {
#if !UNITY_WSA || UNITY_EDITOR
                return "Data Source=" + LocalDatabasePath + ";Version=3;";
#else
                return LocalDatabasePath;
#endif
            }
        }

        /// <summary>
        /// Initializes the local SQLite database.
        /// </summary>
        /// <param name="azureClient">The Azure Client object.</param>
        /// <returns></returns>
        public static async Task Init(MobileServiceClient azureClient)
        {
            if (azureClient == null)
            {
                throw new NullReferenceException("Azure Client is null.");
            }

            _azureClient = azureClient;

            if (!azureClient.SyncContext.IsInitialized)
            {
                try
                {
                    if (!File.Exists(LocalDatabasePath))
                    {
                        string original = Path.Combine(Application.streamingAssetsPath, LocalDatabaseName);
                        if (Application.platform == RuntimePlatform.Android)
                        {
                            WWW reader = new WWW(original);
                            while (!reader.isDone) { }
                            File.WriteAllBytes(LocalDatabasePath, reader.bytes);
                        }
                        else
                        {
                            File.Copy(original, LocalDatabasePath);
                        }
                    }

                    _localStore = new MobileServiceSQLiteStore(LocalDatabaseConnectionString);

                    DefineTables();

                    await _azureClient.SyncContext.InitializeAsync(_localStore);
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                    Debug.LogError(e.Source);
                    Debug.LogError(e.StackTrace);
                    Debug.LogError(e.ToString());
                }
            }
        }

        private static void DefineTables()
        {
            // EDIT - Add your own tables here...
            _localStore.DefineTable<TodoItem>();
        }

        /// <summary>
        /// Syncs with the remote Azure App Service (pull/push operations).
        /// </summary>
        /// <returns></returns>
        public static async Task Sync()
        {
            try
            {
                await Push();
                await Pull();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                Debug.LogError(e.Source);
                Debug.LogError(e.StackTrace);
                Debug.LogError(e.ToString());
            }
        }

        /// <summary>
        /// Pulls the data from the remote Azure App Service and stores them into the local database.
        /// </summary>
        /// <returns></returns>
        public static async Task Pull()
        {
            // EDIT - Add your own tables here.
            MobileAppsTableDAO<TodoItem> todoTableDAO = new MobileAppsTableDAO<TodoItem>(_azureClient);

            await todoTableDAO.Pull(new CancellationToken(), "todoItems", (x) => x.Id != null);
        }

        /// <summary>
        /// Pushes the data stored in the local SQLite database to the remote Azure App Service.
        /// </summary>
        /// <returns></returns>
        public static async Task Push()
        {
            await _azureClient.SyncContext.PushAsync();
        }
    }
}
