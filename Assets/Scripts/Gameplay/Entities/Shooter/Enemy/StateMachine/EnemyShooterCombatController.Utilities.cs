/*
 * [공통 정적 보조 함수]
 * 높이를 제외한 거리/방향 계산, 소리 우선순위/중복 키, 호환용 bool 프로퍼티 조회를 모았습니다.
 * 캐릭터 필드를 직접 바꾸지 않고 전달된 입력을 가공합니다.
 * 실제 전투 흐름을 바꿀 때는 이 파일보다 해당 기능 Domain을 먼저 확인하세요.
 *
 * 이 파일도 EnemyShooterCombatController의 일부입니다. 별도 컴포넌트가 아닙니다.
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public partial class EnemyShooterCombatController
{
    /// <summary>프로퍼티/소유자 존재 여부와 bool 타입을 확인한 뒤 값을 읽습니다. getter 내부 예외를 처리하는 함수는 아닙니다.</summary>
    private static bool TryReadBooleanProperty(System.Reflection.PropertyInfo property, object owner, out bool value)
    {
        value = false;
        if (property == null || owner == null || property.PropertyType != typeof(bool))
        {
            return false;
        }

        object propertyValue = property.GetValue(owner);
        if (!(propertyValue is bool boolValue))
            return false;
        value = boolValue;
        return true;
    }

    /// <summary>두 월드 위치의 높이를 무시한 XZ 거리를 구합니다.</summary>
    private static float GetPlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    /// <summary>높이 성분을 제거한 뒤 길이가 충분하면 단위 방향을 구합니다.</summary>
    private static bool TryNormalizePlanar(Vector3 value, out Vector3 direction)
    {
        value.y = 0f;
        if (value.sqrMagnitude <= 0.0001f)
        {
            direction = default;
            return false;
        }

        direction = value.normalized;
        return true;
    }

    /// <summary>소리 종류를 고정 우선순위로 변환합니다. 폭발 > 총성 > 충돌 > 탄환 스침 > 발소리 순서입니다.</summary>
    private static int ResolveSoundStimulusPriority(SoundStimulusType type)
    {
        return type switch
        {
            SoundStimulusType.Explosion => 500,
            SoundStimulusType.Gunshot => 400,
            SoundStimulusType.ProjectileImpact => 300,
            SoundStimulusType.Impact => 300,
            SoundStimulusType.ProjectileFlyby => 200,
            SoundStimulusType.Footstep => 100,
            _ => 0
        };
    }

    /// <summary>발소리보다 우선순위가 높은 자극이면 조사 재진입 제한 우회를 허용합니다.</summary>
    private static bool CanBypassInvestigationReentry(int priority)
    {
        return priority > ResolveSoundStimulusPriority(SoundStimulusType.Footstep);
    }

    /// <summary>소리 발신자와 자극 종류를 합쳐 중복 검사에 쓰는 키를 만듭니다.</summary>
    private static long BuildSoundStimulusKey(int emitterId, SoundStimulusType type)
    {
        return ((long)emitterId << 32) ^ (uint)type;
    }

    /// <summary>제곱근 계산 없이 두 위치의 XZ 거리 제곱을 구합니다.</summary>
    private static float PlanarSqrDistance(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float z = a.z - b.z;
        return x * x + z * z;
    }
}
