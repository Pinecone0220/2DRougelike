using UnityEngine;

public static class GameSessionData
{
    // 세션이 활성화되어 있는지 확인
    public static bool isSessionActive = false;

    // 저장된 플레이어 스탯
    public static float playerSpd;
    public static float playerJmpSpd;
    public static float playerDoubleJmpSpd;
    public static int playerDmg;
    public static float playerMaxHp;
    public static float playerCurHp;
    public static float playerMaxStamina;
    public static float playerCurStamina;
    public static float playerClearTime;
    public static float playerShield;

    // 세션 데이터 초기화(처음부터 새로 시작할 때 호출)
    public static void ResetSession()
    {
        isSessionActive = false;
        playerClearTime = 0f;
    }

    // 플레이어의 현재 상태를 세션 데이터에 저장
    public static void SavePlayerState(player playerInstance)
    {
        if (playerInstance == null) return;

        isSessionActive = true;
        playerSpd = playerInstance.spd;
        playerJmpSpd = playerInstance.jmp_spd;
        playerDoubleJmpSpd = playerInstance.double_jmp_spd;
        playerDmg = playerInstance.dmg;
        playerMaxHp = playerInstance.maxHp;
        playerCurHp = playerInstance.curHp;
        playerMaxStamina = playerInstance.max_Stamina;
        playerCurStamina = playerInstance.cur_Stamina;
        playerClearTime = playerInstance.clear_Time;
        playerShield = playerInstance.shield;
    }

    // 세션 데이터를 플레이어에게 복원
    public static void LoadPlayerState(player playerInstance)
    {
        if (playerInstance == null || !isSessionActive) return;

        playerInstance.spd = playerSpd;
        playerInstance.jmp_spd = playerJmpSpd;
        playerInstance.double_jmp_spd = playerDoubleJmpSpd;
        playerInstance.dmg = playerDmg;
        playerInstance.maxHp = playerMaxHp;
        playerInstance.curHp = playerCurHp;
        playerInstance.max_Stamina = playerMaxStamina;
        playerInstance.cur_Stamina = playerCurStamina;
        playerInstance.clear_Time = playerClearTime;
        playerInstance.shield = playerShield;
    }
}
