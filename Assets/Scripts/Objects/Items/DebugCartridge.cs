using Assets.Scripts;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using System.Collections.Generic;
using System.Text;
using System;
using TMPro;
using UnityEngine;
using Assets.Scripts.Objects.Electrical;

namespace EntropyFix.Assets.Scripts.Objects.Items
{
    public class DebugCartridge : Cartridge
    {
        public static bool PrefabGeneration = false;
        [SerializeField]
        private TextMeshProUGUI _displayTextMesh;
        public static List<DebugCartridge> AllDebugCartridges = [];
        private static string _notApplicableString = "N/A";
        private string _selectedText = string.Empty;
        private string _outputText = string.Empty;
        private Device _scannedDevice;
        private Device _lastScannedDevice;
        private bool _needTopScroll;

        public Device ScannedDevice => !RootParent || !RootParent.HasAuthority || !CursorManager.CursorThing ? null : (CursorManager.CursorThing as Device);

        public DebugCartridge()
        {
            Slots = [];
        }
        public override void Awake()
        {
            if(PrefabGeneration)
            {
                Slots = [];
                RigidBody = gameObject.GetComponent<Rigidbody>();
                OnPrefabLoad();
            }
            base.Awake();
        }
        public override void OnAssignedReference()
        {
            base.OnAssignedReference();
            AllDebugCartridges.Add(this);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            AllDebugCartridges.Remove(this);
        }

        public void ReadLogicText()
        {
            _scannedDevice = ScannedDevice;
            lock (_outputText)
            {
                if (_scannedDevice != null)
                {
                    if (_lastScannedDevice != _scannedDevice)
                        _needTopScroll = true;
                    _lastScannedDevice = _scannedDevice;
                    _selectedText = _scannedDevice.DisplayName.ToUpper();
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.Append("ReferenceId");
                    stringBuilder.Append("<pos=50%>");
                    stringBuilder.AppendFormat("<color=#20B2AA>${0}</color>", _scannedDevice.ReferenceId.ToString("X"));
                    for (int index = 0; index < EnumCollections.LogicTypes.Length; ++index)
                    {
                        LogicType logicType = EnumCollections.LogicTypes.Values[index];
                        if (_scannedDevice.CanLogicRead(logicType))
                        {
                            stringBuilder.Append("\n");
                            stringBuilder.Append(EnumCollections.LogicTypes.Names[index]);
                            stringBuilder.Append("<pos=50%>");
                            stringBuilder.Append(_scannedDevice.CanLogicWrite(logicType) ? "<color=grey>" : "<color=green>");
                            stringBuilder.Append(Math.Round(_scannedDevice.GetLogicValue(logicType), 3, MidpointRounding.AwayFromZero));
                            stringBuilder.Append("</color>");
                        }
                    }
                    _outputText = stringBuilder.ToString();
                }
                else
                {
                    _selectedText = _notApplicableString;
                    _outputText = string.Empty;
                }
            }
        }

        public override void OnScreenUpdate()
        {
            base.OnScreenUpdate();
            if (_needTopScroll)
            {
                _needTopScroll = false;
                _scrollPanel.SetScrollPosition(0.0f);
            }
            SelectedTitle.text = _selectedText;
            _displayTextMesh.text = _outputText;
            _scrollPanel.SetContentHeight(_displayTextMesh.preferredHeight);
        }
    }
}
