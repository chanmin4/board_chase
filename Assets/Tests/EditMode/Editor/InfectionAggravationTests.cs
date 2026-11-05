using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class InfectionAggravationTests
{
    [Test]
    public void CanonicalDefinitionIsAnActualStatusAndKeepsExistingAssetGuid()
    {
        var definition=AssetDatabase.LoadAssetAtPath<InfectionAggravationStatusSO>("Assets/Resources/InfectionAggravation.asset");
        Assert.That(definition,Is.InstanceOf<StatusEffectDefinitionSO>());
        Assert.That(definition.PersistentStacks,Is.True);
        Assert.That(definition.DealsInfectionDamage,Is.True);
        Assert.That(definition.DamageTickInterval,Is.EqualTo(5));
        Assert.That(definition.DamagePerStackPerSecond,Is.Zero);
        Assert.That(definition.Settings.maximumHealthLossPerStack,Is.EqualTo(5));
        Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(definition)),Is.EqualTo("e0281028000000000000000000000003"));
    }

    [Test]
    public void ActiveStatusReferencesTheSameSavedPayloadRatherThanDuplicatingStacks()
    {
        var definition=AssetDatabase.LoadAssetAtPath<InfectionAggravationStatusSO>("Assets/Resources/InfectionAggravation.asset");
        var data=new InfectionExposureSaveData{stacks=2,gauge=25};
        var active=new EntityStatusEffectRuntime.ActiveStatusEffect(definition,float.PositiveInfinity,data);
        Assert.That(active.UsesAccumulationState(data),Is.True);
        active.SetStacks(3);Assert.That(data.stacks,Is.EqualTo(3));
        data.gauge=50;Assert.That(active.Gauge,Is.EqualTo(50));
        active.TickRemaining=2;Assert.That(data.nextTick,Is.EqualTo(2));
    }

    [Test]
    public void NaturalRecoveryOnlyRemovesPartialGaugeAndNeverTicksDamage()
    {
        var state=new InfectionExposureSaveData{stacks=2,gauge=50};
        state.recoveryDelayRemaining=5;
        Assert.That(InfectionAggravationMath.Tick(state,5,100,5,1,.25f),Is.Zero);
        Assert.That(state.gauge,Is.EqualTo(50));
        Assert.That(InfectionAggravationMath.Tick(state,4,100,5,1,.25f),Is.Zero);
        Assert.That(state.stacks,Is.EqualTo(2));Assert.That(state.gauge,Is.EqualTo(49));
    }

    [Test]
    public void PlayerEquipmentAndStorageAreAuthoredAndDoNotOverlap()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/PlayerInventoryPanel.prefab");
        var ui=new SerializedObject(prefab.GetComponent<PlayerInventoryUI>());
        Assert.That(ui.FindProperty("_headSlot").objectReferenceValue,Is.TypeOf<EquipmentInventoryGasMaskSlotUI>());
        Assert.That(ui.FindProperty("_armorSlot").objectReferenceValue,Is.Not.Null);
        Assert.That(ui.FindProperty("_weaponSlots").arraySize,Is.EqualTo(2));
        Assert.That(ui.FindProperty("_ammoSlots").arraySize,Is.EqualTo(3));
        Assert.That(ui.FindProperty("_passiveSlots").arraySize,Is.EqualTo(8));
        var area=prefab.transform.Find("PlayerInventorySection/InventoryArea");
        var equipment=(RectTransform)area.Find("EquipmentSection");
        var pocket=(RectTransform)area.Find("PocketSection");
        var inventory=(RectTransform)area.Find("InventorySection");
        Assert.That(equipment.anchoredPosition.x,Is.EqualTo(24));
        Assert.That(-equipment.anchoredPosition.y,Is.EqualTo(InventorySplitLayout.EquipmentTop));
        Assert.That(equipment.sizeDelta.y,Is.EqualTo(InventorySplitLayout.EquipmentHeight));
        var passive=(RectTransform)prefab.transform.Find("PassiveSection");
        Assert.That(passive.sizeDelta.y,Is.EqualTo(InventorySplitLayout.PassiveHeight));
        Assert.That(-equipment.anchoredPosition.y,Is.GreaterThanOrEqualTo(-passive.anchoredPosition.y+passive.sizeDelta.y+12));
        var gear=new System.Collections.Generic.List<Component>{
            (Component)ui.FindProperty("_armorSlot").objectReferenceValue,
            (Component)ui.FindProperty("_headSlot").objectReferenceValue};
        foreach(var field in new[]{"_weaponSlots","_ammoSlots"})
        {
            var slots=ui.FindProperty(field);
            for(int i=0;i<slots.arraySize;i++)gear.Add((Component)slots.GetArrayElementAtIndex(i).objectReferenceValue);
        }
        for(int i=0;i<gear.Count;i++)
        {
            var slot=(RectTransform)gear[i].transform;
            Assert.That(slot.anchoredPosition.y,Is.EqualTo(i<4?-8:-108));
            Assert.That(slot.localScale.x,Is.EqualTo(i<4?.56f:.68f));
            Assert.That(slot.anchoredPosition.x+slot.sizeDelta.x*slot.localScale.x,Is.LessThanOrEqualTo(260));
        }
        Assert.That(pocket.anchoredPosition.x,Is.EqualTo(InventorySplitLayout.StorageLeft));
        Assert.That(inventory.anchoredPosition.x,Is.EqualTo(pocket.anchoredPosition.x));
        Assert.That(-inventory.anchoredPosition.y,Is.GreaterThanOrEqualTo(-pocket.anchoredPosition.y+pocket.sizeDelta.y+12));
        Assert.That(inventory.sizeDelta.y,Is.EqualTo(InventorySplitLayout.InventoryHeight));
        Assert.That(ShooterStatPolicy.IsPassiveStat(PlayerStatId.InfectionDefense),Is.False);
        Assert.That(ShooterStatPolicy.IsPassiveStat(PlayerStatId.GasInfectionResistance),Is.False);
    }
}
