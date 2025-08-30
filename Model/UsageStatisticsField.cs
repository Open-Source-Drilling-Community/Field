using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NORCE.Drilling.Field.Model
{
    public struct CountPerDay
    {
        public DateTime Date { get; set; }
        public ulong Count { get; set; }
        /// <summary>
        /// default constructor
        /// </summary>
        public CountPerDay() { }
        /// <summary>
        /// initialization constructor
        /// </summary>
        /// <param name="date"></param>
        /// <param name="count"></param>
        public CountPerDay(DateTime date, ulong count)
        {
            Date = date;
            Count = count;
        }
    }

    public class History
    {
        public List<CountPerDay> Data { get; set; } = new List<CountPerDay>();
        /// <summary>
        /// default constructor
        /// </summary>
        public History()
        {
            if (Data == null)
            {
                Data = new List<CountPerDay>();
            }
        }

        public void Increment()
        {
            if (Data.Count == 0)
            {
                Data.Add(new CountPerDay(DateTime.UtcNow.Date, 1));
            }
            else
            {
                if (Data[Data.Count - 1].Date < DateTime.UtcNow.Date)
                {
                    Data.Add(new CountPerDay(DateTime.UtcNow.Date, 1));
                }
                else
                {
                    Data[Data.Count - 1] = new CountPerDay(Data[Data.Count - 1].Date, Data[Data.Count - 1].Count + 1);
                }
            }
        }
    }
    public class UsageStatisticsField
    {
        public static readonly string HOME_DIRECTORY = ".." + Path.DirectorySeparatorChar + "home" + Path.DirectorySeparatorChar;

        public DateTime LastSaved { get; set; } = DateTime.MinValue;
        public TimeSpan BackUpInterval { get; set; } = TimeSpan.FromMinutes(5);

        public History GetAllFieldIdPerDay { get; set; } = new History();
        public History GetAllFieldMetaInfoPerDay { get; set; } = new History();
        public History GetFieldByIdPerDay { get; set; } = new History();
        public History GetAllFieldLightPerDay { get; set; } = new History();
        public History GetAllFieldPerDay { get; set; } = new History();
        public History PostFieldPerDay { get; set; } = new History();
        public History PutFieldByIdPerDay { get; set; } = new History();
        public History DeleteFieldByIdPerDay { get; set; } = new History();

        public History GetAllFieldCartographicConversionSetIdPerDay { get; set; } = new History();
        public History GetAllFieldCartographicConversionSetMetaInfoPerDay { get; set; } = new History();
        public History GetFieldCartographicConversionSetByIdPerDay { get; set; } = new History();
        public History GetAllFieldCartographicConversionSetLightPerDay { get; set; } = new History();
        public History GetAllFieldCartographicConversionSetPerDay { get; set; } = new History();
        public History PostFieldCartographicConversionSetPerDay { get; set; } = new History();
        public History PutFieldCartographicConversionSetByIdPerDay { get; set; } = new History();
        public History DeleteFieldCartographicConversionSetByIdPerDay { get; set; } = new History();

        private static object lock_ = new object();

        private static UsageStatisticsField? instance_ = null;

        public static UsageStatisticsField Instance
        {
            get
            {
                if (instance_ == null)
                {
                    if (File.Exists(HOME_DIRECTORY + "history.json"))
                    {
                        try
                        {
                            string? jsonStr = null;
                            lock (lock_)
                            {
                                using (StreamReader reader = new StreamReader(HOME_DIRECTORY + "history.json"))
                                {
                                    jsonStr = reader.ReadToEnd();
                                }
                                if (!string.IsNullOrEmpty(jsonStr))
                                {
                                    instance_ = JsonSerializer.Deserialize<UsageStatisticsField>(jsonStr);
                                }
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    if (instance_ == null)
                    {
                        instance_ = new UsageStatisticsField();
                    }
                }
                return instance_;
            }
        }

        public void IncrementGetAllFieldIdPerDay()
        {
            lock (lock_)
            {
                if (GetAllFieldIdPerDay == null)
                {
                    GetAllFieldIdPerDay = new History();
                }
                GetAllFieldIdPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetAllFieldMetaInfoPerDay()
        {
            lock (lock_)
            {
                if (GetAllFieldMetaInfoPerDay == null)
                {
                    GetAllFieldMetaInfoPerDay = new History();
                }
                GetAllFieldMetaInfoPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetFieldByIdPerDay()
        {
            lock (lock_)
            {
                if (GetFieldByIdPerDay == null)
                {
                    GetFieldByIdPerDay = new History();
                }
                GetFieldByIdPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetAllFieldLightPerDay()
        {
            lock (lock_)
            {
                if (GetAllFieldLightPerDay == null)
                {
                    GetAllFieldLightPerDay = new History();
                }
                GetAllFieldLightPerDay.Increment();
                ManageBackup();
            }
        }

        public void IncrementPostFieldPerDay()
        {
            lock (lock_)
            {
                if (PostFieldPerDay == null)
                {
                    PostFieldPerDay = new History();
                }
                PostFieldPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetAllFieldPerDay()
        {
            lock (lock_)
            {
                if (GetAllFieldPerDay == null)
                {
                    GetAllFieldPerDay = new History();
                }
                GetAllFieldPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementPutFieldByIdPerDay()
        {
            lock (lock_)
            {
                if (PutFieldByIdPerDay == null)
                {
                    PutFieldByIdPerDay = new History();
                }
                PutFieldByIdPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementDeleteFieldByIdPerDay()
        {
            lock (lock_)
            {
                if (DeleteFieldByIdPerDay == null)
                {
                    DeleteFieldByIdPerDay = new History();
                }
                DeleteFieldByIdPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetAllFieldCartographicConversionSetIdPerDay()
        {
            lock (lock_)
            {
                if (GetAllFieldCartographicConversionSetIdPerDay == null)
                {
                    GetAllFieldCartographicConversionSetIdPerDay = new History();
                }
                GetAllFieldCartographicConversionSetIdPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetAllFieldCartographicConversionSetMetaInfoPerDay()
        {
            lock (lock_)
            {
                if (GetAllFieldCartographicConversionSetMetaInfoPerDay == null)
                {
                    GetAllFieldCartographicConversionSetMetaInfoPerDay = new History();
                }
                GetAllFieldCartographicConversionSetMetaInfoPerDay.Increment();
                ManageBackup();
            }
        }

        public void IncrementGetFieldCartographicConversionSetByIdPerDay()
        {
            lock (lock_)
            {
                if (GetFieldCartographicConversionSetByIdPerDay == null)
                {
                    GetFieldCartographicConversionSetByIdPerDay = new History();
                }
                GetFieldCartographicConversionSetByIdPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetAllFieldCartographicConversionSetLightPerDay()
        {
            lock (lock_)
            {
                if (GetAllFieldCartographicConversionSetLightPerDay == null)
                {
                    GetAllFieldCartographicConversionSetLightPerDay = new History();
                }
                GetAllFieldCartographicConversionSetLightPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetAllFieldCartographicConversionSetPerDay()
        {
            lock (lock_)
            {
                if (GetAllFieldCartographicConversionSetPerDay == null)
                {
                    GetAllFieldCartographicConversionSetPerDay = new History();
                }
                GetAllFieldCartographicConversionSetPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementPostFieldCartographicConversionSetPerDay()
        {
            lock (lock_)
            {
                if (PostFieldCartographicConversionSetPerDay == null)
                {
                    PostFieldCartographicConversionSetPerDay = new History();
                }
                PostFieldCartographicConversionSetPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementPutFieldCartographicConversionSetByIdPerDay()
        {
            lock (lock_)
            {
                if (PutFieldCartographicConversionSetByIdPerDay == null)
                {
                    PutFieldCartographicConversionSetByIdPerDay = new History();
                }
                PutFieldCartographicConversionSetByIdPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementDeleteFieldCartographicConversionSetByIdPerDay()
        {
            lock (lock_)
            {
                if (DeleteFieldCartographicConversionSetByIdPerDay == null)
                {
                    DeleteFieldCartographicConversionSetByIdPerDay = new History();
                }
                DeleteFieldCartographicConversionSetByIdPerDay.Increment();
                ManageBackup();
            }
        }

        private void ManageBackup()
        {
            if (DateTime.UtcNow > LastSaved + BackUpInterval)
            {
                LastSaved = DateTime.UtcNow;
                try
                {
                    string jsonStr = JsonSerializer.Serialize(this);
                    if (!string.IsNullOrEmpty(jsonStr) && Directory.Exists(HOME_DIRECTORY))
                    {
                        using (StreamWriter writer = new StreamWriter(HOME_DIRECTORY + "history.json"))
                        {
                            writer.Write(jsonStr);
                            writer.Flush();
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }
    }
}