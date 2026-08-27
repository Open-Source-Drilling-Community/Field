using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace OSDC.Drilling.Field.Model
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

        public History GetAllFieldDelineationLineTypeIdPerDay { get; set; } = new History();
        public History GetAllFieldDelineationLineTypeMetaInfoPerDay { get; set; } = new History();
        public History GetFieldDelineationLineTypeByIdPerDay { get; set; } = new History();
        public History GetAllFieldDelineationLineTypePerDay { get; set; } = new History();
        public History PostFieldDelineationLineTypePerDay { get; set; } = new History();
        public History PutFieldDelineationLineTypeByIdPerDay { get; set; } = new History();
        public History DeleteFieldDelineationLineTypeByIdPerDay { get; set; } = new History();

        public History GetAllFieldFeatureCategoryIdPerDay { get; set; } = new History();
        public History GetAllFieldFeatureCategoryMetaInfoPerDay { get; set; } = new History();
        public History GetFieldFeatureCategoryByIdPerDay { get; set; } = new History();
        public History GetAllFieldFeatureCategoryPerDay { get; set; } = new History();
        public History PostFieldFeatureCategoryPerDay { get; set; } = new History();
        public History PutFieldFeatureCategoryByIdPerDay { get; set; } = new History();
        public History DeleteFieldFeatureCategoryByIdPerDay { get; set; } = new History();

        public History GetAllFieldIdentityIdPerDay { get; set; } = new History();
        public History GetAllFieldIdentityMetaInfoPerDay { get; set; } = new History();
        public History GetFieldIdentityByIdPerDay { get; set; } = new History();
        public History GetAllFieldIdentityPerDay { get; set; } = new History();
        public History PostFieldIdentityPerDay { get; set; } = new History();
        public History PutFieldIdentityByIdPerDay { get; set; } = new History();
        public History DeleteFieldIdentityByIdPerDay { get; set; } = new History();

        public History GetAllFieldMembershipCategoryIdPerDay { get; set; } = new History();
        public History GetAllFieldMembershipCategoryMetaInfoPerDay { get; set; } = new History();
        public History GetFieldMembershipCategoryByIdPerDay { get; set; } = new History();
        public History GetAllFieldMembershipCategoryPerDay { get; set; } = new History();
        public History PostFieldMembershipCategoryPerDay { get; set; } = new History();
        public History PutFieldMembershipCategoryByIdPerDay { get; set; } = new History();
        public History DeleteFieldMembershipCategoryByIdPerDay { get; set; } = new History();

        public History GetFieldUsageStatisticsPerDay { get; set; } = new History();

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
                        catch (Exception)
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
        public void IncrementGetAllFieldDelineationLineTypeIdPerDay() => IncrementHistory(() => GetAllFieldDelineationLineTypeIdPerDay, value => GetAllFieldDelineationLineTypeIdPerDay = value);
        public void IncrementGetAllFieldDelineationLineTypeMetaInfoPerDay() => IncrementHistory(() => GetAllFieldDelineationLineTypeMetaInfoPerDay, value => GetAllFieldDelineationLineTypeMetaInfoPerDay = value);
        public void IncrementGetFieldDelineationLineTypeByIdPerDay() => IncrementHistory(() => GetFieldDelineationLineTypeByIdPerDay, value => GetFieldDelineationLineTypeByIdPerDay = value);
        public void IncrementGetAllFieldDelineationLineTypePerDay() => IncrementHistory(() => GetAllFieldDelineationLineTypePerDay, value => GetAllFieldDelineationLineTypePerDay = value);
        public void IncrementPostFieldDelineationLineTypePerDay() => IncrementHistory(() => PostFieldDelineationLineTypePerDay, value => PostFieldDelineationLineTypePerDay = value);
        public void IncrementPutFieldDelineationLineTypeByIdPerDay() => IncrementHistory(() => PutFieldDelineationLineTypeByIdPerDay, value => PutFieldDelineationLineTypeByIdPerDay = value);
        public void IncrementDeleteFieldDelineationLineTypeByIdPerDay() => IncrementHistory(() => DeleteFieldDelineationLineTypeByIdPerDay, value => DeleteFieldDelineationLineTypeByIdPerDay = value);

        public void IncrementGetAllFieldFeatureCategoryIdPerDay() => IncrementHistory(() => GetAllFieldFeatureCategoryIdPerDay, value => GetAllFieldFeatureCategoryIdPerDay = value);
        public void IncrementGetAllFieldFeatureCategoryMetaInfoPerDay() => IncrementHistory(() => GetAllFieldFeatureCategoryMetaInfoPerDay, value => GetAllFieldFeatureCategoryMetaInfoPerDay = value);
        public void IncrementGetFieldFeatureCategoryByIdPerDay() => IncrementHistory(() => GetFieldFeatureCategoryByIdPerDay, value => GetFieldFeatureCategoryByIdPerDay = value);
        public void IncrementGetAllFieldFeatureCategoryPerDay() => IncrementHistory(() => GetAllFieldFeatureCategoryPerDay, value => GetAllFieldFeatureCategoryPerDay = value);
        public void IncrementPostFieldFeatureCategoryPerDay() => IncrementHistory(() => PostFieldFeatureCategoryPerDay, value => PostFieldFeatureCategoryPerDay = value);
        public void IncrementPutFieldFeatureCategoryByIdPerDay() => IncrementHistory(() => PutFieldFeatureCategoryByIdPerDay, value => PutFieldFeatureCategoryByIdPerDay = value);
        public void IncrementDeleteFieldFeatureCategoryByIdPerDay() => IncrementHistory(() => DeleteFieldFeatureCategoryByIdPerDay, value => DeleteFieldFeatureCategoryByIdPerDay = value);

        public void IncrementGetAllFieldIdentityIdPerDay() => IncrementHistory(() => GetAllFieldIdentityIdPerDay, value => GetAllFieldIdentityIdPerDay = value);
        public void IncrementGetAllFieldIdentityMetaInfoPerDay() => IncrementHistory(() => GetAllFieldIdentityMetaInfoPerDay, value => GetAllFieldIdentityMetaInfoPerDay = value);
        public void IncrementGetFieldIdentityByIdPerDay() => IncrementHistory(() => GetFieldIdentityByIdPerDay, value => GetFieldIdentityByIdPerDay = value);
        public void IncrementGetAllFieldIdentityPerDay() => IncrementHistory(() => GetAllFieldIdentityPerDay, value => GetAllFieldIdentityPerDay = value);
        public void IncrementPostFieldIdentityPerDay() => IncrementHistory(() => PostFieldIdentityPerDay, value => PostFieldIdentityPerDay = value);
        public void IncrementPutFieldIdentityByIdPerDay() => IncrementHistory(() => PutFieldIdentityByIdPerDay, value => PutFieldIdentityByIdPerDay = value);
        public void IncrementDeleteFieldIdentityByIdPerDay() => IncrementHistory(() => DeleteFieldIdentityByIdPerDay, value => DeleteFieldIdentityByIdPerDay = value);

        public void IncrementGetAllFieldMembershipCategoryIdPerDay() => IncrementHistory(() => GetAllFieldMembershipCategoryIdPerDay, value => GetAllFieldMembershipCategoryIdPerDay = value);
        public void IncrementGetAllFieldMembershipCategoryMetaInfoPerDay() => IncrementHistory(() => GetAllFieldMembershipCategoryMetaInfoPerDay, value => GetAllFieldMembershipCategoryMetaInfoPerDay = value);
        public void IncrementGetFieldMembershipCategoryByIdPerDay() => IncrementHistory(() => GetFieldMembershipCategoryByIdPerDay, value => GetFieldMembershipCategoryByIdPerDay = value);
        public void IncrementGetAllFieldMembershipCategoryPerDay() => IncrementHistory(() => GetAllFieldMembershipCategoryPerDay, value => GetAllFieldMembershipCategoryPerDay = value);
        public void IncrementPostFieldMembershipCategoryPerDay() => IncrementHistory(() => PostFieldMembershipCategoryPerDay, value => PostFieldMembershipCategoryPerDay = value);
        public void IncrementPutFieldMembershipCategoryByIdPerDay() => IncrementHistory(() => PutFieldMembershipCategoryByIdPerDay, value => PutFieldMembershipCategoryByIdPerDay = value);
        public void IncrementDeleteFieldMembershipCategoryByIdPerDay() => IncrementHistory(() => DeleteFieldMembershipCategoryByIdPerDay, value => DeleteFieldMembershipCategoryByIdPerDay = value);

        public void IncrementGetFieldUsageStatisticsPerDay() => IncrementHistory(() => GetFieldUsageStatisticsPerDay, value => GetFieldUsageStatisticsPerDay = value);

        private void IncrementHistory(Func<History?> getHistory, Action<History> setHistory)
        {
            lock (lock_)
            {
                History? history = getHistory();
                if (history == null)
                {
                    history = new History();
                    setHistory(history);
                }

                history.Increment();
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
                    throw new Exception("Failed to save usage statistics to file.", ex);
                }
            }
        }
    }
}
