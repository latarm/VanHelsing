using System;
using System.Collections.Generic;
using System.Data;
using Common;
using Events;
using Events.Args;
using Extensions;
using Quests;
using UnityEngine;

namespace DatabaseWrapper
{
    /// <summary>
    ///     Обертка для получения данных о квестах из базы
    ///     Call Init() before work
    /// </summary>
    /// TODO: Move Locale to global settings
    public static class QuestRepository
    {
        private static Dictionary<int, QuestDto> _cache = new Dictionary<int, QuestDto>();
        private static Locale _locale = Locale.RU;

        private static readonly Dictionary<Locale, (string, string)> _localeTables =
            new Dictionary<Locale, (string, string)>
            {
                //TODO: add another languages
                {Locale.RU, ("quest_locale_ru", "quest_objectives_locale_ru")}
            };

        /// <summary>
        ///     Получает квест по его Id
        /// </summary>
        /// <param name="id">ид квеста</param>
        /// <returns></returns>
        public static QuestDto GetById(int id)
        {
            try
            {
                if (_cache.ContainsKey(id)) return _cache[id];
                var dtQ = DatabaseWrapper.GetTable($"select * from 'quest' where Id={id};");
                var dtLoc = DatabaseWrapper.GetTable(
                    $"select * from '{GetQuestLocaleTable()}' where QuestId={id} limit 1;");
                var dtObj = DatabaseWrapper.GetTable($"select * from 'quest_objectives' where QuestId={id};");
                var dtPoi = DatabaseWrapper.GetTable($"select * from 'quest_poi' where QuestId={id};");
                var dtReq = DatabaseWrapper.GetTable($"select * from 'quest_requirements' where TargetQuestId={id};");
                var dtRew = DatabaseWrapper.GetTable($"select * from 'quest_rewards' where QuestId={id};");

                if (dtQ.Rows.Count == 0)
                {
                    throw new Exception($"Quest with Id[{id}] not found!");
                }
                
                var questDto = new QuestDto
                {
                    Id = id,
                    Title = dtLoc.Rows[0].GetString(QUEST_LOCALE_TITLE),
                    Description = dtLoc.Rows[0].GetString(QUEST_LOCALE_DESCRIPTION),
                    RewardExp = dtQ.Rows[0].GetInt(QUEST_REWARDEXP),
                    RewardMoney = dtQ.Rows[0].GetInt(QUEST_REWARDMONEY),
                    MinLevel = dtQ.Rows[0].GetInt(QUEST_MINLEVEL),
                    QuestLevel = dtQ.Rows[0].GetInt(QUEST_LEVEL),
                    ZoneId = dtQ.Rows[0].GetInt(QUEST_ZONEID),
                    TimeAllowed = dtQ.Rows[0].GetInt(QUEST_TIMEALLOWED),
                    StartDialogId = dtQ.Rows[0].GetInt(QUEST_STARTDIALOGID),
                    EndDialogId = dtQ.Rows[0].GetInt(QUEST_ENDDIALOGID)
                };
                //Requirements
                foreach (DataRow row in dtReq.Rows)
                    questDto.RequiredQuests.Add(row.GetInt(QUEST_REQUIREMENTS_REQUIREDQUEST));
                //Markers
                foreach (DataRow row in dtPoi.Rows)
                {
                    var t = row.GetInt(QUEST_POI_MARKERTYPE);
                    var marker = new QuestMarkerDto
                    {
                        MapId = row.GetInt(QUEST_POI_ZONEID),
                        X = row.GetFloat(QUEST_POI_X),
                        Y = row.GetFloat(QUEST_POI_Y)
                    };
                    switch (t)
                    {
                        case POI_START:
                            questDto.QuestStart = marker;
                            break;
                        case POI_END:
                            questDto.QuestEnd = marker;
                            break;
                        default:
                            questDto.MapMarkers.Add(marker);
                            break;
                    }
                }

                //Objectives
                foreach (DataRow row in dtObj.Rows)
                {
                    var tid = row.GetInt(QUEST_OBJECTIVES_ID);
                    var tmp =
                        DatabaseWrapper.ExecuteQueryWithAnswer(
                            $"select 'Title' from '{GetQuestObjectivesLocaleTable()}' where ObjectiveId={tid} limit 1;");
                    var task = new QuestTaskDto
                    {
                        Id = row.GetInt(QUEST_OBJECTIVES_ID),
                        TargetId = row.GetInt(QUEST_OBJECTIVES_TARGETID),
                        NeededAmount = row.GetInt(QUEST_OBJECTIVES_AMOUNT),
                        Type = (QuestTaskTypes) row.GetInt(QUEST_OBJECTIVES_TYPE),
                        Description = tmp,
                        IsOptional = row.GetInt(QUEST_OBJECTIVES_ISOPTIONAL) == 1
                    };
                }
                //Rewards

                foreach (DataRow row in dtRew.Rows)
                    questDto.Rewards.Add(new QuestRewardDto
                    {
                        ObjectId = row.GetInt(QUEST_REWARDS_REWARDOBJECTID),
                        ObjectCount = row.GetInt(QUEST_REWARDS_REWARDOBJECTCOUNT),
                        ObjectType = (QuestRewardObjectTypes) row.GetInt(QUEST_REWARDS_REWARDOBJECTTYPE),
                        Type = (QuestRewardTypes) row.GetInt(QUEST_REWARDS_REWARDTYPE)
                    });

                _cache.Add(id, questDto);
                return questDto;
            }
            catch (Exception e)
            {
                Debug.LogError($"{DateTime.Now.ToShortTimeString()}    Quest with id[{id}] fires exception    {e}\n");
                throw;
            }
        }

        /// <summary>
        ///     Получаем имя таблицы с локализацией квестов
        /// </summary>
        /// <returns></returns>
        private static string GetQuestLocaleTable()
        {
            return _localeTables.ContainsKey(_locale) ? _localeTables[_locale].Item1 : _localeTables[Locale.RU].Item1;
        }

        /// <summary>
        ///     Получаем имя таблицы с локализацией задач квестов
        /// </summary>
        /// <returns></returns>
        private static string GetQuestObjectivesLocaleTable()
        {
            return _localeTables.ContainsKey(_locale) ? _localeTables[_locale].Item2 : _localeTables[Locale.RU].Item2;
        }

        /// <summary>
        ///     Получаем все квесты в зоне по id зоны
        /// </summary>
        /// <param name="zoneId"></param>
        /// <returns></returns>
        public static IEnumerable<QuestDto> GetAllInZone(int zoneId)
        {
            var dtZ = DatabaseWrapper.GetTable($"select Id from 'quest' where ZoneId={zoneId};");
            foreach (DataRow row in dtZ.Rows) yield return GetById(row.GetInt(0));
        }

        /// <summary>
        ///     Чистим in-memory cache
        /// </summary>
        public static void ClearCache()
        {
            _cache = new Dictionary<int, QuestDto>();
        }

        /// <summary>
        ///     Устанавливаем язык
        /// </summary>
        /// <param name="locale"></param>
        public static void SetLocale(Locale locale)
        {
            _locale = locale;
        }

        /// <summary>
        ///     Подписываемся на события
        /// </summary>
        public static void Init()
        {
            //TODO: Событие смены языка
            EventManager.StartListening(GameEventTypes.QuestReported, OnQuestReported);
        }

        /// <summary>
        ///     Когда квест сдан, он в кэше уже не нужен
        /// </summary>
        /// <param name="arg0"></param>
        private static void OnQuestReported(EventArgs arg0)
        {
            if (!(arg0 is IdArgs idArgs)) return;
            if (_cache.ContainsKey(idArgs.Id))
                _cache.Remove(idArgs.Id);
        }

        /// <summary>
        ///     Получаем язык, который установлен в этом классе
        /// </summary>
        /// <returns></returns>
        public static Locale GetCurrentLocale()
        {
            return _locale;
        }

        #region ColumnsDefinitions

        //table : Quest
        private const byte QUEST_ID = 0;
        private const byte QUEST_MINLEVEL = 1;
        private const byte QUEST_LEVEL = 2;
        private const byte QUEST_TIMEALLOWED = 3;
        private const byte QUEST_ZONEID = 4;
        private const byte QUEST_REWARDEXP = 5;
        private const byte QUEST_REWARDMONEY = 6;
        private const byte QUEST_STARTDIALOGID = 7;
        private const byte QUEST_ENDDIALOGID = 8;

        //table : Quest_locale_xx
        private const byte QUEST_LOCALE_ID = 0;
        private const byte QUEST_LOCALE_QUESTID = 1;
        private const byte QUEST_LOCALE_TITLE = 2;
        private const byte QUEST_LOCALE_DESCRIPTION = 3;

        //table : quest_objectives
        private const byte QUEST_OBJECTIVES_ID = 0;
        private const byte QUEST_OBJECTIVES_QUESTID = 1;
        private const byte QUEST_OBJECTIVES_TYPE = 2;
        private const byte QUEST_OBJECTIVES_TARGETID = 3;
        private const byte QUEST_OBJECTIVES_AMOUNT = 4;
        private const byte QUEST_OBJECTIVES_ISOPTIONAL = 5;

        //table : quest_objectives_locale_xx
        private const byte QUEST_OBJECTIVES_LOCALE_ID = 0;
        private const byte QUEST_OBJECTIVES_LOCALE_OBJECTIVEID = 1;
        private const byte QUEST_OBJECTIVES_LOCALE_TITLE = 2;

        //table : quest_poi
        private const byte QUEST_POI_ID = 0;
        private const byte QUEST_POI_QUESTID = 1;
        private const byte QUEST_POI_ZONEID = 2;
        private const byte QUEST_POI_X = 3;
        private const byte QUEST_POI_Y = 4;
        private const byte QUEST_POI_MARKERTYPE = 5;

        //table : quest_requirements
        private const byte QUEST_REQUIREMENTS_ID = 0;
        private const byte QUEST_REQUIREMENTS_TARGETQUESTID = 1;
        private const byte QUEST_REQUIREMENTS_REQUIREDQUEST = 2;

        //table : quest_rewards
        private const byte QUEST_REWARDS_ID = 0;
        private const byte QUEST_REWARDS_QUESTID = 1;
        private const byte QUEST_REWARDS_REWARDTYPE = 2;
        private const byte QUEST_REWARDS_REWARDOBJECTTYPE = 3;
        private const byte QUEST_REWARDS_REWARDOBJECTID = 4;
        private const byte QUEST_REWARDS_REWARDOBJECTCOUNT = 5;

        #endregion

        #region PoiMarkerType

        private const int POI_TASK = 1;
        private const int POI_START = 2;
        private const int POI_END = 3;

        #endregion
    }
}