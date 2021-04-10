using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace RP.Localization
{
	/// <summary>
	/// Downloads spritesheets from Google Spreadsheet and saves them to Resources. My laziness made me to create it.
	/// </summary>
	public class LocalizationSync : EditorWindow
	{
		

		private const string UrlPattern = "https://docs.google.com/spreadsheets/d/{0}/export?format=csv&gid={1}";

		private static LocalizationSyncSettings settings;
		[MenuItem("Tools/SyncLocalization")]
		public static void ShowWindow()
		{
			LocalizationSync window = (LocalizationSync)EditorWindow.GetWindow(typeof(LocalizationSync), false, "LocalizationSync");
		}
		void OnGUI()
		{
            if (settings == null)
            {
				settings = GetLocalizationSyncSettings();
                if (settings==null)
                {
					settings = new LocalizationSyncSettings();

				}
				
			}
			SetLayout();
		}
		private static async void SetLayout()
		{
			EditorGUILayout.Space();


			var sheets = settings.Sheets;

			EditorGUIUtility.labelWidth = 50;
			settings.TableID =EditorGUILayout.TextField("TableId:", PlayerPrefs.GetString("loc_table_id", settings.TableID));
			EditorGUILayout.LabelField("Sheet IDs");			
			for(int i=0;i< sheets.Count;i++)
            {
				var sheet = sheets[i];
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Name", GUILayout.Width(40));
				string name = EditorGUILayout.TextField("", sheet.Name);
				EditorGUILayout.LabelField("ID", GUILayout.Width(15));
				string id = EditorGUILayout.TextField("", sheet.Id.ToString());
                try
                {
					long thisID = long.Parse(id);
                    if (thisID!= settings.Sheets[i].Id|| settings.Sheets[i].Name!=name)
                    {
						settings.Sheets[i].Id = thisID;
						settings.Sheets[i].Name = name;
					}
					
                }
                catch
                {
					
                }
				
				if (GUILayout.Button("-", GUILayout.MinHeight(15), GUILayout.MinWidth(15)))
				{
					settings.Sheets.RemoveAt(i);
				}
				EditorGUILayout.EndHorizontal();
			}
			if (GUILayout.Button("Add Sheet", GUILayout.MinHeight(25)))
			{
				settings.Sheets.Add(new Sheet());
			}
			if (GUILayout.Button("SAVE", GUILayout.MinHeight(25)))
			{
				SaveLocalizationSyncSettings();
			}
			if (GUILayout.Button("Sync Sheets", GUILayout.MinHeight(25)))
			{
				await SyncSheets();
			}

		}
        private void OnDestroy()
        {
			settings = null;

		}

        private static async Task SyncSheets()
		{
			Debug.Log("<color=red>Sync started, please wait for confirmation message...</color>");

			var dict = new Dictionary<string, UnityWebRequest>();

			foreach (var sheet in settings.Sheets)
			{
				var url = string.Format(UrlPattern, settings.TableID, sheet.Id);

				Debug.LogFormat("Downloading: {0}...", url);

				using (var webRequest = UnityWebRequest.Get(url))
                {
					UnityWebRequestAsyncOperation asyncOperation = webRequest.SendWebRequest();

					asyncOperation.completed += (o) =>
					{
						if (webRequest.error == null)
						{
							//var sheet = Sheets.Single(i => url == string.Format(UrlPattern, TableId, i.Id));
							string folderPath = "Assets/Resources/Localization";
							if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
							var path = System.IO.Path.Combine(folderPath, sheet.Name + ".csv");
							System.IO.File.WriteAllBytes(path, webRequest.downloadHandler.data);
							Debug.LogFormat("Sheet {0} downloaded to {1}", sheet.Id, path);
						}
						else
						{
							Debug.LogFormat("webRequest.error : {0} with {1}", sheet.Id, webRequest.error);
						}
					};
					while (!asyncOperation.isDone)
					{
						await Task.Delay(100);
					}
				}
			}
			AssetDatabase.Refresh();
			Debug.Log("<color=green>Localization successfully synced!</color>");
		}
		
		private static LocalizationSyncSettings GetLocalizationSyncSettings()
		{
			string fullFilePath = Application.persistentDataPath + "/Localization/Localization.json";
			if (System.IO.File.Exists(fullFilePath))
			{
				string saveData = System.IO.File.ReadAllText(fullFilePath);
				if (!string.IsNullOrEmpty(saveData))
				{
					return JsonUtility.FromJson<LocalizationSyncSettings>(saveData);
				}
			}
			return null;


		}
		private static void SaveLocalizationSyncSettings()
		{
			string data = JsonUtility.ToJson(settings);
			string folderPath = Application.persistentDataPath + "/Localization/";
			if (!Directory.Exists(folderPath))Directory.CreateDirectory(folderPath);
			System.IO.File.WriteAllText(folderPath+"Localization.json", data);
		}


	}
	[Serializable]
	public class LocalizationSyncSettings
	{

		public string TableID = string.Empty;
		public List<Sheet> Sheets = new List<Sheet>();



	}
	[Serializable]
	public class Sheet
	{
		public string Name;
		public long Id;
	}
}
