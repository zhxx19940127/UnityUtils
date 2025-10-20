using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
///     时间工具类
///     提供时间戳转换、时间格式化、时间计算等功能
/// </summary>
public static class DateTimeUtil
{
    #region 时间戳相关

    /// <summary>
    ///     时间戳计时开始时间（Unix纪元时间）
    /// </summary>
    private static readonly DateTime TimeStampStartTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    ///     DateTime转换为10位时间戳（单位：秒）
    /// </summary>
    /// <param name="dateTime">DateTime对象</param>
    /// <returns>10位时间戳（单位：秒）</returns>
    public static long DateTimeToTimeStamp(DateTime dateTime)
    {
        return (long)(dateTime.ToUniversalTime() - TimeStampStartTime).TotalSeconds;
    }

    /// <summary>
    ///     DateTime转换为13位时间戳（单位：毫秒）
    /// </summary>
    /// <param name="dateTime">DateTime对象</param>
    /// <returns>13位时间戳（单位：毫秒）</returns>
    public static long DateTimeToLongTimeStamp(DateTime dateTime)
    {
        return (long)(dateTime.ToUniversalTime() - TimeStampStartTime).TotalMilliseconds;
    }

    /// <summary>
    ///     10位时间戳（单位：秒）转换为DateTime
    /// </summary>
    /// <param name="timeStamp">10位时间戳（单位：秒）</param>
    /// <returns>DateTime对象</returns>
    public static DateTime TimeStampToDateTime(long timeStamp)
    {
        return TimeStampStartTime.AddSeconds(timeStamp).ToLocalTime();
    }

    /// <summary>
    ///     13位时间戳（单位：毫秒）转换为DateTime
    /// </summary>
    /// <param name="longTimeStamp">13位时间戳（单位：毫秒）</param>
    /// <returns>DateTime对象</returns>
    public static DateTime LongTimeStampToDateTime(long longTimeStamp)
    {
        return TimeStampStartTime.AddMilliseconds(longTimeStamp).ToLocalTime();
    }

    /// <summary>
    ///     获取当前时间的10位时间戳（秒）
    /// </summary>
    /// <returns>当前时间的10位时间戳</returns>
    public static long GetCurrentTimeStamp()
    {
        return DateTimeToTimeStamp(DateTime.Now);
    }

    /// <summary>
    ///     获取当前时间的13位时间戳（毫秒）
    /// </summary>
    /// <returns>当前时间的13位时间戳</returns>
    public static long GetCurrentLongTimeStamp()
    {
        return DateTimeToLongTimeStamp(DateTime.Now);
    }

    #endregion

    #region 时间格式化

    /// <summary>
    ///     常用时间格式字符串
    /// </summary>
    public static class Formats
    {
        public const string Date = "yyyy-MM-dd";
        public const string Time = "HH:mm:ss";
        public const string DateTime = "yyyy-MM-dd HH:mm:ss";
        public const string DateTimeWithMilliseconds = "yyyy-MM-dd HH:mm:ss.fff";
        public const string ISO8601 = "yyyy-MM-ddTHH:mm:ss.fffZ";
        public const string Chinese = "yyyy年MM月dd日 HH时mm分ss秒";
        public const string FileName = "yyyyMMdd_HHmmss";
        public const string CompactDateTime = "yyyyMMddHHmmss";
    }

    /// <summary>
    ///     格式化DateTime为指定格式字符串
    /// </summary>
    /// <param name="dateTime">要格式化的DateTime</param>
    /// <param name="format">格式字符串，默认为标准日期时间格式</param>
    /// <returns>格式化后的字符串</returns>
    public static string FormatDateTime(DateTime dateTime, string format = Formats.DateTime)
    {
        return dateTime.ToString(format);
    }

    /// <summary>
    ///     格式化时间戳为指定格式字符串
    /// </summary>
    /// <param name="timeStamp">时间戳（秒）</param>
    /// <param name="format">格式字符串</param>
    /// <returns>格式化后的字符串</returns>
    public static string FormatTimeStamp(long timeStamp, string format = Formats.DateTime)
    {
        return FormatDateTime(TimeStampToDateTime(timeStamp), format);
    }

    /// <summary>
    ///     格式化长时间戳为指定格式字符串
    /// </summary>
    /// <param name="longTimeStamp">时间戳（毫秒）</param>
    /// <param name="format">格式字符串</param>
    /// <returns>格式化后的字符串</returns>
    public static string FormatLongTimeStamp(long longTimeStamp, string format = Formats.DateTime)
    {
        return FormatDateTime(LongTimeStampToDateTime(longTimeStamp), format);
    }

    #endregion

    #region 时间解析

    /// <summary>
    ///     尝试解析字符串为DateTime
    /// </summary>
    /// <param name="dateTimeString">时间字符串</param>
    /// <param name="dateTime">解析结果</param>
    /// <returns>是否解析成功</returns>
    public static bool TryParseDateTime(string dateTimeString, out DateTime dateTime)
    {
        return DateTime.TryParse(dateTimeString, out dateTime);
    }

    /// <summary>
    ///     使用指定格式解析时间字符串
    /// </summary>
    /// <param name="dateTimeString">时间字符串</param>
    /// <param name="format">格式字符串</param>
    /// <param name="dateTime">解析结果</param>
    /// <returns>是否解析成功</returns>
    public static bool TryParseDateTime(string dateTimeString, string format, out DateTime dateTime)
    {
        return DateTime.TryParseExact(dateTimeString, format, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out dateTime);
    }

    #endregion

    #region 时间计算

    /// <summary>
    ///     计算两个时间之间的差值
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns>时间差</returns>
    public static TimeSpan GetTimeDifference(DateTime startTime, DateTime endTime)
    {
        return endTime - startTime;
    }

    /// <summary>
    ///     计算距离指定时间还有多长时间
    /// </summary>
    /// <param name="targetTime">目标时间</param>
    /// <returns>时间差</returns>
    public static TimeSpan GetTimeUntil(DateTime targetTime)
    {
        return targetTime - DateTime.Now;
    }

    /// <summary>
    ///     计算从指定时间到现在过了多长时间
    /// </summary>
    /// <param name="fromTime">起始时间</param>
    /// <returns>时间差</returns>
    public static TimeSpan GetTimeSince(DateTime fromTime)
    {
        return DateTime.Now - fromTime;
    }

    /// <summary>
    ///     获取本周开始时间（周一）
    /// </summary>
    /// <param name="dateTime">参考时间</param>
    /// <returns>本周开始时间</returns>
    public static DateTime GetWeekStart(DateTime dateTime)
    {
        var diff = (7 + (dateTime.DayOfWeek - DayOfWeek.Monday)) % 7;
        return dateTime.AddDays(-1 * diff).Date;
    }

    /// <summary>
    ///     获取本月开始时间
    /// </summary>
    /// <param name="dateTime">参考时间</param>
    /// <returns>本月开始时间</returns>
    public static DateTime GetMonthStart(DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1);
    }

    /// <summary>
    ///     获取本年开始时间
    /// </summary>
    /// <param name="dateTime">参考时间</param>
    /// <returns>本年开始时间</returns>
    public static DateTime GetYearStart(DateTime dateTime)
    {
        return new DateTime(dateTime.Year, 1, 1);
    }

    #endregion

    #region 时间判断

    /// <summary>
    ///     判断是否为同一天
    /// </summary>
    /// <param name="date1">时间1</param>
    /// <param name="date2">时间2</param>
    /// <returns>是否为同一天</returns>
    public static bool IsSameDay(DateTime date1, DateTime date2)
    {
        return date1.Date == date2.Date;
    }

    /// <summary>
    ///     判断是否为今天
    /// </summary>
    /// <param name="dateTime">要判断的时间</param>
    /// <returns>是否为今天</returns>
    public static bool IsToday(DateTime dateTime)
    {
        return IsSameDay(dateTime, DateTime.Now);
    }

    /// <summary>
    ///     判断是否为昨天
    /// </summary>
    /// <param name="dateTime">要判断的时间</param>
    /// <returns>是否为昨天</returns>
    public static bool IsYesterday(DateTime dateTime)
    {
        return IsSameDay(dateTime, DateTime.Now.AddDays(-1));
    }

    /// <summary>
    ///     判断是否为本周
    /// </summary>
    /// <param name="dateTime">要判断的时间</param>
    /// <returns>是否为本周</returns>
    public static bool IsThisWeek(DateTime dateTime)
    {
        var weekStart = GetWeekStart(DateTime.Now);
        return dateTime.Date >= weekStart && dateTime.Date < weekStart.AddDays(7);
    }

    /// <summary>
    ///     判断是否为本月
    /// </summary>
    /// <param name="dateTime">要判断的时间</param>
    /// <returns>是否为本月</returns>
    public static bool IsThisMonth(DateTime dateTime)
    {
        var now = DateTime.Now;
        return dateTime.Year == now.Year && dateTime.Month == now.Month;
    }

    /// <summary>
    ///     判断是否为本年
    /// </summary>
    /// <param name="dateTime">要判断的时间</param>
    /// <returns>是否为本年</returns>
    public static bool IsThisYear(DateTime dateTime)
    {
        return dateTime.Year == DateTime.Now.Year;
    }

    /// <summary>
    ///     判断是否为工作日（周一到周五）
    /// </summary>
    /// <param name="dateTime">要判断的时间</param>
    /// <returns>是否为工作日</returns>
    public static bool IsWeekday(DateTime dateTime)
    {
        return dateTime.DayOfWeek >= DayOfWeek.Monday && dateTime.DayOfWeek <= DayOfWeek.Friday;
    }

    /// <summary>
    ///     判断是否为周末（周六或周日）
    /// </summary>
    /// <param name="dateTime">要判断的时间</param>
    /// <returns>是否为周末</returns>
    public static bool IsWeekend(DateTime dateTime)
    {
        return dateTime.DayOfWeek == DayOfWeek.Saturday || dateTime.DayOfWeek == DayOfWeek.Sunday;
    }

    #endregion

    #region 友好时间显示

    /// <summary>
    ///     获取友好的时间描述（如"刚刚"、"5分钟前"、"昨天"等）
    /// </summary>
    /// <param name="dateTime">要描述的时间</param>
    /// <returns>友好的时间描述</returns>
    public static string GetFriendlyTimeDescription(DateTime dateTime)
    {
        var timeDiff = DateTime.Now - dateTime;

        if (timeDiff.TotalSeconds < 60) return "刚刚";

        if (timeDiff.TotalMinutes < 60) return $"{(int)timeDiff.TotalMinutes}分钟前";

        if (timeDiff.TotalHours < 24) return $"{(int)timeDiff.TotalHours}小时前";

        if (IsYesterday(dateTime)) return "昨天";

        if (timeDiff.TotalDays < 7) return $"{(int)timeDiff.TotalDays}天前";

        if (IsThisYear(dateTime)) return dateTime.ToString("MM月dd日");

        return dateTime.ToString("yyyy年MM月dd日");
    }

    /// <summary>
    ///     获取友好的时间戳描述
    /// </summary>
    /// <param name="timeStamp">时间戳（秒）</param>
    /// <returns>友好的时间描述</returns>
    public static string GetFriendlyTimeDescription(long timeStamp)
    {
        return GetFriendlyTimeDescription(TimeStampToDateTime(timeStamp));
    }

    #endregion

    #region 时区转换

    /// <summary>
    ///     转换为UTC时间
    /// </summary>
    /// <param name="dateTime">本地时间</param>
    /// <returns>UTC时间</returns>
    public static DateTime ToUtc(DateTime dateTime)
    {
        return dateTime.ToUniversalTime();
    }

    /// <summary>
    ///     转换为本地时间
    /// </summary>
    /// <param name="utcDateTime">UTC时间</param>
    /// <returns>本地时间</returns>
    public static DateTime ToLocal(DateTime utcDateTime)
    {
        return utcDateTime.ToLocalTime();
    }

    #endregion

    #region 时间范围

    /// <summary>
    ///     判断时间是否在指定范围内
    /// </summary>
    /// <param name="dateTime">要判断的时间</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns>是否在范围内</returns>
    public static bool IsInRange(DateTime dateTime, DateTime startTime, DateTime endTime)
    {
        return dateTime >= startTime && dateTime <= endTime;
    }

    /// <summary>
    ///     获取两个时间之间的所有日期
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>日期列表</returns>
    public static List<DateTime> GetDateRange(DateTime startDate, DateTime endDate)
    {
        var dates = new List<DateTime>();
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1)) dates.Add(date);

        return dates;
    }

    #endregion
}