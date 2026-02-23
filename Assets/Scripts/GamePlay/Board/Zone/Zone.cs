using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

[DisallowMultipleComponent]
public class Zone : MonoBehaviour
{
       public int id;
        public int profileIndex;
        public Vector2Int center;
        public List<Vector2Int> tiles;
        public float xp;
        public float remaintime;
        public float time_to_live;
        public int curhit;                 // ★ 누적 히트(존별)
        public int reqHit;             // 요구 튕김(종류별 고정)
        public float enterBonus;
        public float gainPerSec;
        public Vector2Int footprint;
        public Vector3 centerWorld;
        public float radiusWorld;
        public Material domeMat;
        public Material ringMat;

        public float consumeUnlockTime = 0f;
        public bool mustExitFirst = false;

        public float bonusAngleDeg;          // 0~360, 존 중심에서 바라보는 방향
        public float bonusNextRefreshAt;     // >0이면 해당 시각에 각도 리롤
        public int RemainingHit => Mathf.Max(0, reqHit - curhit);

    public void Init(int _id, int _profile, Vector3 cW, float rW)
    {
        id = _id;
        profileIndex = _profile;
        centerWorld = cW; 
         radiusWorld = rW;
        transform.position = new Vector3(cW.x, transform.position.y, cW.z);
    }
}
