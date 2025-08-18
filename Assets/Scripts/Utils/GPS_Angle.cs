using UnityEngine;


public enum GPS_TYPE
{
    LONGITUDE, // 经度
    LATITUDE // 纬度
}

public class GPS_Angle
{
    // 角度单位枚举
    public enum UNIT
    {
        RAD,
        DEG
    } // 弧度/度

    // 标准化模式枚举  
    public enum NORM
    {
        POS,
        NEG
    } // 正角度/可负角度

    private delegate float NormalizeDelegate(float angle, NORM norm, GPS_TYPE type = GPS_TYPE.LONGITUDE);

    private static NormalizeDelegate[] NormalizeFunc = new NormalizeDelegate[2] { NormalizeRad, NormalizeDeg };


    public const float PI_DIV2 = Mathf.PI * 0.5f;

    public const float PI = Mathf.PI;

    public const float PI_34 = Mathf.PI * 1.5f;

    public const float PI_MUL2 = Mathf.PI * 2.0f;


    private float m_value;

    private UNIT m_unit;


    public GPS_Angle(float value, UNIT unit)
    {
        m_value = value;
        m_unit = unit;
    }

    public GPS_Angle(GPS_Angle gpsAngle)
    {
        m_value = gpsAngle.m_value;
        m_unit = gpsAngle.m_unit;
    }

    public float As(UNIT unit)
    {
        if (m_unit != unit)
        {
            return (m_unit == UNIT.RAD) ? (m_value * Mathf.Rad2Deg) : (m_value * Mathf.Rad2Deg);
        }

        return m_value;
    }

    public float Value
    {
        get { return m_value; }
        set { m_value = value; }
    }

    public float Rad
    {
        get { return As(UNIT.RAD); }
        set
        {
            m_value = value;
            m_unit = UNIT.RAD;
        }
    }

    public float Deg
    {
        get { return As(UNIT.DEG); }
        set
        {
            m_value = value;
            m_unit = UNIT.DEG;
        }
    }

    // 度制标准化
    static public float NormalizeDeg(float angle, NORM norm, GPS_TYPE type = GPS_TYPE.LONGITUDE)
    {
        int revs = (int)(angle / 360.0f);

        angle -= ((float)revs * 360.0f);

        if (angle < 0.0f) angle += 360.0f;


        if ((norm == NORM.NEG) && (angle > 180.0f)) angle -= 360.0f;

        if (type == GPS_TYPE.LATITUDE)
        {
            if (angle > 270.0f) angle = -360.0f + angle;

            else if (angle > 180.0f) angle = -180.0f + angle;

            else if (angle > 90.0f) angle = 180.0f - angle;

            else if (angle < -90.0f) angle = -180.0f - angle;
        }

        return angle;
    }

    // 弧度制标准化
    static public float NormalizeRad(float angle, NORM norm, GPS_TYPE type = GPS_TYPE.LONGITUDE)
    {
        int revs = (int)(angle / PI_MUL2);

        angle -= ((float)revs * PI_MUL2);

        if (angle < 0.0f) angle += PI_MUL2;


        if ((norm == NORM.NEG) && (angle > PI)) angle -= PI_MUL2;

        if (type == GPS_TYPE.LATITUDE)
        {
            if (angle > PI_34) angle = -PI_MUL2 + angle;

            else if (angle > PI) angle = -PI + angle;

            else if (angle > PI_DIV2) angle = PI - angle;

            else if (angle < -PI_DIV2) angle = -PI - angle;
        }

        return angle;
    }

    // 通用标准化
    public static float Normalize(float angle, UNIT unit, NORM norm, GPS_TYPE type = GPS_TYPE.LONGITUDE)
    {
        return NormalizeFunc[(int)unit](angle, norm, type);
    }
}