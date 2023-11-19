﻿using Sandbox.Engine.Utils;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // CONFIG
        public static readonly bool IS_CONSUMER = false;
        public static readonly bool IS_PROVIDER = false;
        public static readonly bool IS_RU = true;
        public static readonly string COMMAND_CONSUMER_ORDER = "order";
        public static readonly string TEXT_PANEL = "LCD [sex_Trans]";
        public static readonly string PROVIDER_CONNECTOR = "Connector [sex_Trans]";
        public static readonly string MESSAGE_TAG_BROADCAST = "sex_Trans::broadcast::handshake";
        public static readonly string MESSAGE_TAG_UNICAST_ORDER = "sex_Trans::unicast::order";

        public static readonly ImmutableDictionary<ItemType, long> DEFAULT_QUANTITIES = new Dictionary<ItemType, long>()
        {
            { ItemType.Component_SteelPlate, 10 },
            { ItemType.Component_SolarCell, 5 },
            { ItemType.Component_SmallTube, 10 },
            { ItemType.Component_LargeTube, 10 },
            { ItemType.Component_Girder, 10 },
            { ItemType.Component_Construction, 10 },
            { ItemType.Component_InteriorPlate, 10 },
            { ItemType.Component_Display, 5 },
            { ItemType.Component_BulletproofGlass, 5 },
            { ItemType.Component_Motor, 10 },
            { ItemType.Component_Computer, 10 },
            { ItemType.Component_Detector, 5 },
            { ItemType.Component_RadioCommunication, 5 },
            { ItemType.Component_MetalGrid, 10 },
            { ItemType.Component_Superconductor, 10 },
            { ItemType.Component_PowerCell, 10 },
            { ItemType.Component_Thrust, 10 },
            { ItemType.Component_GravityGenerator, 0 },
            { ItemType.Component_ZoneChip, 0 },
        }.ToImmutableDictionary();

        public static readonly ImmutableDictionary<ItemType, string> ITEM_TYPE_TO_NAME = new Dictionary<ItemType, string>() {
            { ItemType.Component_SteelPlate, "SteelPlate" },
            { ItemType.Component_SmallTube, "SmallTube" },
            { ItemType.Component_LargeTube, "LargeTube" },
            { ItemType.Component_Motor, "Motor" },
            { ItemType.Component_PowerCell, "PowerCell" },
            { ItemType.Component_Superconductor, "Superconductor" },
            { ItemType.Component_GravityGenerator, "GravityGenerator" },
            { ItemType.Component_Girder, "Girder" },
            { ItemType.Component_Canvas, "Canvas" },
            { ItemType.Component_Thrust, "Thrust" },
            { ItemType.Component_RadioCommunication, "RadioCommunication" },
            { ItemType.Component_InteriorPlate, "InteriorPlate" },
            { ItemType.Component_Computer, "Computer" },
            { ItemType.Component_Detector, "Detector" },
            { ItemType.Component_Display, "Display" },
            { ItemType.Component_BulletproofGlass, "BulletproofGlass" },
            { ItemType.Component_SolarCell, "SolarCell" },
            { ItemType.Component_MetalGrid, "MetalGrid" },
            { ItemType.Component_Construction, "Construction" },
            { ItemType.Component_Reactor, "Reactor" },
            { ItemType.Component_ZoneChip, "ZoneChip" },
            { ItemType.PhysicalObject_SpaceCredit, "SpaceCredit" },
            { ItemType.OxygenContainerObject_OxygenBottle, "OxygenBottle" },
            { ItemType.GasContainerObject_HydrogenBottle, "HydrogenBottle" },
            { ItemType.AmmoMagazine_SemiAutoPistolMagazine, "SemiAutoPistolMagazine" },
            { ItemType.PhysicalGunObject_SemiAutoPistolItem, "SemiAutoPistolItem" },
            { ItemType.AmmoMagazine_NATO_25x184mm, "NATO_25x184mm" },
            { ItemType.AmmoMagazine_LargeCalibreAmmo_02, "LargeCalibreAmmo_02" },
        }.ToImmutableDictionary();

        public static readonly ImmutableDictionary<ItemType, string> RU_ITEM_NAMES = new Dictionary<ItemType, string>() {
            { ItemType.Component_SteelPlate, "СтальнаПластина" },
            { ItemType.Component_SmallTube, "МалаяТрубка" },
            { ItemType.Component_LargeTube, "БольшаяТрубка" },
            { ItemType.Component_InteriorPlate, "ВнутренняяПластина" },
            { ItemType.Component_Construction, "СтроительныеКомпоненты" },
            { ItemType.Component_Motor, "Мотор" },
            { ItemType.Component_PowerCell, "Энергоячейка" },
            { ItemType.Component_Computer, "Компьютер" },
            { ItemType.Component_Display, "Экран" },
            { ItemType.Component_Girder, "Балка" },
            { ItemType.Component_SolarCell, "СолнечнаяЯчейка" },
            { ItemType.Component_BulletproofGlass, "БронированноеСтекло" },
            { ItemType.Component_RadioCommunication, "РадиоКомпоненты" },
            { ItemType.Component_Detector, "КомпонентыДетектор" },
            { ItemType.Component_MetalGrid, "КомпонентРешётки" },
            { ItemType.Component_Superconductor, "Сверхпроводник" },
            { ItemType.Component_GravityGenerator, "КомпонентыГравитационногоГенератора" },
            { ItemType.Component_Reactor, "КомпонентыРеактора" },
            { ItemType.Component_Thrust, "ИонныйУскоритель" },
            { ItemType.Component_Canvas, "ПолотноПарашюта" },
            { ItemType.Component_ZoneChip, "КлючБезопасности" },
            { ItemType.PhysicalObject_SpaceCredit, "Космокредит" },
            { ItemType.OxygenContainerObject_OxygenBottle, "КислородныйБалон" },
            { ItemType.GasContainerObject_HydrogenBottle, "ВодородныйБалон" },
            { ItemType.AmmoMagazine_SemiAutoPistolMagazine, "БоеприпасыПистолета" },
            { ItemType.PhysicalGunObject_SemiAutoPistolItem, "Пистолет" },
            { ItemType.AmmoMagazine_NATO_25x184mm, "БоеприпасыНАТО" },
            { ItemType.AmmoMagazine_LargeCalibreAmmo_02, "БоеприпасыКрупнокалиберные" },
        }.ToImmutableDictionary();

        public static readonly ImmutableDictionary<ItemType, string> EN_ITEM_NAMES = new Dictionary<ItemType, string>() {
            { ItemType.Component_SteelPlate, "SteelPlate" },
            { ItemType.Component_SmallTube, "SmallTube" },
            { ItemType.Component_LargeTube, "LargeTube" },
            { ItemType.Component_Motor, "Motor" },
            { ItemType.Component_PowerCell, "PowerCell" },
            { ItemType.Component_Superconductor, "Superconductor" },
            { ItemType.Component_GravityGenerator, "GravityGenerator" },
            { ItemType.Component_Girder, "Girder" },
            { ItemType.Component_Canvas, "Canvas" },
            { ItemType.Component_Thrust, "Thrust" },
            { ItemType.Component_RadioCommunication, "RadioCommunication" },
            { ItemType.Component_InteriorPlate, "InteriorPlate" },
            { ItemType.Component_Computer, "Computer" },
            { ItemType.Component_Detector, "Detector" },
            { ItemType.Component_Display, "Display" },
            { ItemType.Component_BulletproofGlass, "BulletproofGlass" },
            { ItemType.Component_SolarCell, "SolarCell" },
            { ItemType.Component_MetalGrid, "MetalGrid" },
            { ItemType.Component_Construction, "Construction" },
            { ItemType.Component_Reactor, "Reactor" },
            { ItemType.Component_ZoneChip, "ZoneChip" },
            { ItemType.PhysicalObject_SpaceCredit, "SpaceCredit" },
            { ItemType.OxygenContainerObject_OxygenBottle, "OxygenBottle" },
            { ItemType.GasContainerObject_HydrogenBottle, "HydrogenBottle" },
            { ItemType.AmmoMagazine_SemiAutoPistolMagazine, "SemiAutoPistolMagazine" },
            { ItemType.PhysicalGunObject_SemiAutoPistolItem, "SemiAutoPistolItem" },
            { ItemType.AmmoMagazine_NATO_25x184mm, "NATO_25x184mm" },
            { ItemType.AmmoMagazine_LargeCalibreAmmo_02, "LargeCalibreAmmo_02" },
        }.ToImmutableDictionary();
        // END OF CONFIG

        //{ "Камень", "Ore/Stone" },
        //{ "Кремний", "Ore/Silicon" },
        //{ "Железо", "Ore/Iron" },
        //{ "Лёд", "Ore/Ice" },
        //{ "Никель", "Ore/Nickel" },
        //{ "Серебро", "Ore/Silver" },
        //{ "Золото", "Ore/Gold" },
        //{ "Платина", "Ore/Platinum" },
        //{ "Кобальт", "Ore/Cobalt" },
        //{ "Уран", "Ore/Uranium" },
        //{ "Скрап", "Ore/Scrap" },
        //{ "Магний", "Ore/Magnesium" },

        public Logger logger;

        private readonly Consumer _consumer;
        private readonly Provider _provider;

        public Program()
        {
            logger = new Logger(GetTextPanel(), Echo);

            if (IS_CONSUMER)
            {
                _consumer = new Consumer(this);
            }

            if (IS_PROVIDER)
            {
                _provider = new Provider(this);
            }
        }

        public void Main(string argument, UpdateType updateType)
        {
            if ("clear_console".Equals(argument))
            {
                logger.Clear();
                return;
            }

            _provider?.Main(argument, updateType);
            _consumer?.Main(argument, updateType);
        }

        private IMyTextPanel GetTextPanel()
        {
            return Utils.GetBlock<IMyTextPanel>(GridTerminalSystem, TEXT_PANEL, "Text Panel");
        }

        public enum ItemType
        {
            Component_SteelPlate,
            Component_SmallTube,
            Component_LargeTube,
            Component_Motor,
            Component_PowerCell,
            Component_Superconductor,
            Component_GravityGenerator,
            Component_Girder,
            Component_Canvas,
            Component_Thrust,
            Component_RadioCommunication,
            Component_InteriorPlate,
            Component_Computer,
            Component_Detector,
            Component_Display,
            Component_BulletproofGlass,
            Component_SolarCell,
            Component_MetalGrid,
            Component_Construction,
            Component_Reactor,
            Component_ZoneChip,
            PhysicalObject_SpaceCredit,
            OxygenContainerObject_OxygenBottle,
            GasContainerObject_HydrogenBottle,
            AmmoMagazine_SemiAutoPistolMagazine,
            PhysicalGunObject_SemiAutoPistolItem,
            AmmoMagazine_NATO_25x184mm,
            AmmoMagazine_LargeCalibreAmmo_02,
        }

        public static ItemType? GetItemType(string displayName)
        {
            foreach (var item in RU_ITEM_NAMES)
            {
                if (item.Value.Equals(displayName))
                {
                    return item.Key;
                }
            }

            foreach (var item in EN_ITEM_NAMES)
            {
                if (item.Value.Equals(displayName))
                {
                    return item.Key;
                }
            }

            return null;
        }
    }
}
