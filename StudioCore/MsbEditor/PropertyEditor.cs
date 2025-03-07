﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Numerics;
using SoulsFormats;
using ImGuiNET;
using System.Net.Http.Headers;
using System.Security;
using System.Text.RegularExpressions;

namespace StudioCore.MsbEditor
{
    public class PropertyEditor
    {
        public ActionManager ContextActionManager;

        private Dictionary<string, PropertyInfo[]> _propCache = new Dictionary<string, PropertyInfo[]>();

        private string _refContextCurrentAutoComplete = "";

        public PropertyEditor(ActionManager manager)
        {
            ContextActionManager = manager;
        }

        private bool PropertyRow(Type typ, object oldval, out object newval, bool isBool)
        {
            try
            {
                if (isBool)
                {
                    dynamic val = oldval;
                    bool checkVal = val > 0;
                    if (ImGui.Checkbox("##valueBool", ref checkVal))
                    {
                        newval = Convert.ChangeType(checkVal ? 1 : 0, oldval.GetType());
                        return true;
                    }
                    ImGui.SameLine();
                }
            }
            catch
            {

            }

            if (typ == typeof(long))
            {
                long val = (long)oldval;
                string strval = $@"{val}";
                if (ImGui.InputText("##value", ref strval, 128))
                {
                    var res = long.TryParse(strval, out val);
                    if (res)
                    {
                        newval = val;
                        return true;
                    }
                }
            }
            else if (typ == typeof(int))
            {
                int val = (int)oldval;
                if (ImGui.InputInt("##value", ref val))
                {
                    newval = val;
                    return true;
                }
            }
            else if (typ == typeof(uint))
            {
                uint val = (uint)oldval;
                string strval = $@"{val}";
                if (ImGui.InputText("##value", ref strval, 16))
                {
                    var res = uint.TryParse(strval, out val);
                    if (res)
                    {
                        newval = val;
                        return true;
                    }
                }
            }
            else if (typ == typeof(short))
            {
                int val = (short)oldval;
                if (ImGui.InputInt("##value", ref val))
                {
                    newval = (short)val;
                    return true;
                }
            }
            else if (typ == typeof(ushort))
            {
                ushort val = (ushort)oldval;
                string strval = $@"{val}";
                if (ImGui.InputText("##value", ref strval, 5))
                {
                    var res = ushort.TryParse(strval, out val);
                    if (res)
                    {
                        newval = val;
                        return true;
                    }
                }
            }
            else if (typ == typeof(sbyte))
            {
                int val = (sbyte)oldval;
                if (ImGui.InputInt("##value", ref val))
                {
                    newval = (sbyte)val;
                    return true;
                }
            }
            else if (typ == typeof(byte))
            {
                byte val = (byte)oldval;
                string strval = $@"{val}";
                if (ImGui.InputText("##value", ref strval, 3))
                {
                    var res = byte.TryParse(strval, out val);
                    if (res)
                    {
                        newval = val;
                        return true;
                    }
                }
            }
            else if (typ == typeof(bool))
            {
                bool val = (bool)oldval;
                if (ImGui.Checkbox("##value", ref val))
                {
                    newval = val;
                    return true;
                }
            }
            else if (typ == typeof(float))
            {
                float val = (float)oldval;
                if (ImGui.DragFloat("##value", ref val, 0.1f))
                {
                    newval = val;
                    return true;
                    // shouldUpdateVisual = true;
                }
            }
            else if (typ == typeof(string))
            {
                string val = (string)oldval;
                if (val == null)
                {
                    val = "";
                }
                if (ImGui.InputText("##value", ref val, 128))
                {
                    newval = val;
                    return true;
                }
            }
            else if (typ == typeof(Vector2))
            {
                Vector2 val = (Vector2)oldval;
                if (ImGui.DragFloat2("##value", ref val, 0.1f))
                {
                    newval = val;
                    return true;
                    // shouldUpdateVisual = true;
                }
            }
            else if (typ == typeof(Vector3))
            {
                Vector3 val = (Vector3)oldval;
                if (ImGui.DragFloat3("##value", ref val, 0.1f))
                {
                    newval = val;
                    return true;
                    // shouldUpdateVisual = true;
                }
            }
            else
            {
                ImGui.Text("ImplementMe");
            }

            newval = null;
            return false;
        }

        private void UpdateProperty(object prop, object obj, object newval,
            bool changed, bool committed, int arrayindex = -1)
        {
            if (changed)
            {
                ChangeProperty(prop, obj, newval, ref committed, arrayindex);
            }
        }

        private void ChangeProperty(object prop, object obj, object newval,
            ref bool committed, int arrayindex = -1)
        {
            if (committed)
            {
                PropertiesChangedAction action;
                if (arrayindex != -1)
                {
                    action = new PropertiesChangedAction((PropertyInfo)prop, arrayindex, obj, newval);
                }
                else
                {
                    action = new PropertiesChangedAction((PropertyInfo)prop, obj, newval);
                }
                ContextActionManager.ExecuteAction(action);
            }
        }

        public void PropEditorParamRow(PARAM.Row row, ref string propSearchString)
        {
            IReadOnlyList<PARAM.Cell> cells = new List<PARAM.Cell>();
            cells = row.Cells;
            ImGui.Columns(2);
            ImGui.Separator();
            int id = 0;

            if (propSearchString != null)
                ImGui.InputText("Search...", ref propSearchString, 255);
            Regex propSearchRx = null;
            try
            {
                propSearchRx = new Regex(propSearchString.ToLower());
            }
            catch
            {
            }
            ImGui.NextColumn();
            ImGui.NextColumn();

            // This should be rewritten somehow it's super ugly
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.8f, 0.8f, 0.8f, 1.0f));
            var nameProp = row.GetType().GetProperty("Name");
            var idProp = row.GetType().GetProperty("ID");
            PropEditorPropInfoRow(row, nameProp, "Name", ref id, propSearchRx);
            PropEditorPropInfoRow(row, idProp, "ID", ref id, propSearchRx);
            ImGui.PopStyleColor();

            ParamMetaData meta  = ParamMetaData.Get(row.Def);
            if (meta != null && meta.AlternateOrder != null && ParamEditorScreen.AllowFieldReorderPreference)
            {
                foreach (var field in meta.AlternateOrder)
                {
                    if (field.Equals("-"))
                    {
                        ImGui.Separator();
                        continue;
                    }
                    if (row[field] == null)
                        continue;
                    PropEditorPropCellRow(row[field], ref id, propSearchRx);
                }
                foreach (var cell in cells)
                {
                    if (!meta.AlternateOrder.Contains(cell.Def.InternalName))
                        PropEditorPropCellRow(cell, ref id, propSearchRx);
                }
            }
            else
            {
                foreach (var cell in cells)
                {
                    PropEditorPropCellRow(cell, ref id, propSearchRx);
                }
            }
            ImGui.Columns(1);
        }
        
        // Many parameter options, which may be simplified.
        private void PropEditorPropInfoRow(PARAM.Row row, PropertyInfo prop, string visualName, ref int id, Regex propSearchRx)
        {
            PropEditorPropRow(prop.GetValue(row), ref id, visualName, null, prop.PropertyType, prop, null, row, propSearchRx);
        }
        private void PropEditorPropCellRow(PARAM.Cell cell, ref int id, Regex propSearchRx)
        {
            PropEditorPropRow(cell.Value, ref id, cell.Def.InternalName, FieldMetaData.Get(cell.Def), cell.Value.GetType(), cell.GetType().GetProperty("Value"), cell, null, propSearchRx);
        }
        private void PropEditorPropRow(object oldval, ref int id, string internalName, FieldMetaData cellMeta, Type propType, PropertyInfo proprow, PARAM.Cell nullableCell, PARAM.Row nullableRow, Regex propSearchRx)
        {
            List<string> RefTypes = cellMeta == null ? null : cellMeta.RefTypes;
            string VirtualRef = cellMeta == null ? null : cellMeta.VirtualRef;
            ParamEnum Enum = cellMeta == null ? null : cellMeta.EnumType;
            string Wiki = cellMeta == null ? null : cellMeta.Wiki;
            bool IsBool = cellMeta == null ? false : cellMeta.IsBool;
            string AltName = cellMeta == null ? null : cellMeta.AltName;

            if (propSearchRx != null)
            {
                if (!propSearchRx.IsMatch(internalName.ToLower()) && !(AltName != null && propSearchRx.IsMatch(AltName.ToLower())))
                {
                    return;
                }
            }

            object newval = null;
            ImGui.PushID(id);
            ImGui.AlignTextToFramePadding();
            PropertyRowName(ref internalName, cellMeta);
            PropertyRowNameContextMenu(internalName, cellMeta);
            if (Wiki != null)
            {
                if (UIHints.AddImGuiHintButton(internalName, ref Wiki))
                    cellMeta.Wiki = Wiki;
            }

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 0.0f, 1.0f));
            if (ParamEditorScreen.HideReferenceRowsPreference == false && RefTypes != null)
                ImGui.TextUnformatted($@"  <{String.Join(',', RefTypes)}>");
            if (ParamEditorScreen.HideEnumsPreference == false && Enum != null)
                ImGui.TextUnformatted($@"  {Enum.name}");
            ImGui.PopStyleColor();

            //PropertyRowMetaDefContextMenu();
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            bool changed = false;

            bool matchDefault = nullableCell != null && nullableCell.Def.Default.Equals(oldval);
            if (matchDefault)
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.75f, 0.75f, 0.75f, 1.0f));
            else if ((ParamEditorScreen.HideReferenceRowsPreference == false && RefTypes != null) || (ParamEditorScreen.HideEnumsPreference == false && Enum != null) || VirtualRef != null)
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.5f, 1.0f, 1.0f));

            changed = PropertyRow(propType, oldval, out newval, IsBool);
            bool committed = ImGui.IsItemDeactivatedAfterEdit();
            if ((ParamEditorScreen.HideReferenceRowsPreference == false && RefTypes != null) || (ParamEditorScreen.HideEnumsPreference == false && Enum != null) || VirtualRef != null || matchDefault)
                ImGui.PopStyleColor();
            PropertyRowValueContextMenu(internalName, VirtualRef, oldval);

            if (ParamEditorScreen.HideReferenceRowsPreference == false && RefTypes != null)
                PropertyRowRefs(RefTypes, oldval);
            if (ParamEditorScreen.HideEnumsPreference == false && Enum != null)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.5f, 0.5f, 1.0f));
                ImGui.TextUnformatted(Enum.values.GetValueOrDefault(oldval.ToString(), "Not Enumerated"));
                ImGui.PopStyleColor();
            }
            if ((ParamEditorScreen.HideReferenceRowsPreference == false || ParamEditorScreen.HideEnumsPreference == false) && PropertyRowMetaValueContextMenu(oldval, ref newval, RefTypes, Enum))
            {
                changed = true;
                committed = true;
            }

            UpdateProperty(proprow, nullableCell != null ? (object)nullableCell : nullableRow, newval, changed, committed);
            ImGui.NextColumn();
            ImGui.PopID();
            id++;
        }

        private void PropertyRowName(ref string internalName, FieldMetaData cellMeta)
        {
            string AltName = cellMeta == null ? null : cellMeta.AltName;
            if (cellMeta != null && ParamEditorScreen.EditorMode)
            {
                if (AltName != null)
                {
                    ImGui.InputText("", ref AltName, 128);
                    if (ImGui.IsItemDeactivatedAfterEdit())
                        cellMeta.AltName = AltName;
                }
                else
                {
                    ImGui.InputText("", ref internalName, 128);
                    if (ImGui.IsItemDeactivatedAfterEdit())
                        cellMeta.AltName = internalName;
                }
                if (cellMeta.AltName != null && (cellMeta.AltName.Equals(internalName) || cellMeta.AltName.Equals("")))
                    cellMeta.AltName = null;
            }
            else
            {
                string printedName = (AltName != null && ParamEditorScreen.ShowAltNamesPreference) ? (ParamEditorScreen.AlwaysShowOriginalNamePreference ? $"{internalName} ({AltName})" : $"{AltName}*") : internalName;
                ImGui.TextUnformatted(printedName);
            }
        }
        
        private void PropertyRowRefs(List<string> reftypes, dynamic oldval)
        {
            // Add named row and context menu
            // Lists located params
            ImGui.NewLine();
            bool entryFound = false;
            foreach (string rt in reftypes)
            {
                string hint = "";
                if (ParamBank.Params.ContainsKey(rt))
                {
                    PARAM param = ParamBank.Params[rt];
                    ParamMetaData meta = ParamMetaData.Get(ParamBank.Params[rt].AppliedParamdef);
                    if (meta != null && meta.Row0Dummy && (int) oldval == 0)
                        continue;
                    PARAM.Row r = param[(int) oldval];
                    ImGui.SameLine();
                    if (r == null && (int) oldval > 0)
                    {
                        if (meta != null && meta.OffsetSize > 0)
                        {
                            // Test if previous row exists. In future, add param meta to determine size of offset
                            int altval = (int) oldval - (int) oldval % meta.OffsetSize;
                            r = ParamBank.Params[rt][altval];
                            hint = $@"(+{(int) oldval % meta.OffsetSize})";
                        }
                    }
                    if (r == null)
                        continue;
                    entryFound = true;
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.5f, 0.5f, 1.0f));
                    if (r.Name == null || r.Name.Equals(""))
                    {
                        ImGui.TextUnformatted("Unnamed Row");
                    }
                    else
                    {
                        ImGui.TextUnformatted(r.Name + hint);
                    }
                    ImGui.PopStyleColor();
                    ImGui.NewLine();
                }
            }
            ImGui.SameLine();
            if (!entryFound)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                ImGui.TextUnformatted("___");
                ImGui.PopStyleColor();
            }
        }
        private void PropertyRowNameContextMenu(string originalName, FieldMetaData cellMeta)
        {
            if (ImGui.BeginPopupContextItem("rowName"))
            {
                if (ParamEditorScreen.ShowAltNamesPreference == true && ParamEditorScreen.AlwaysShowOriginalNamePreference == false)
                    ImGui.Text(originalName);
                if (ImGui.Selectable("Search..."))
                    EditorCommandQueue.AddCommand($@"param/search/prop {originalName.Replace(" ", "\\s")} ");
                if (ParamEditorScreen.EditorMode && cellMeta != null)
                {
                    if (ImGui.BeginMenu("Add Reference"))
                    {
                        foreach (string p in ParamBank.Params.Keys)
                        {
                            if (ImGui.Selectable(p))
                            {
                                if (cellMeta.RefTypes == null)
                                    cellMeta.RefTypes = new List<string>();
                                cellMeta.RefTypes.Add(p);
                            }
                        }
                        ImGui.EndMenu();
                    }
                    if (cellMeta.RefTypes != null && ImGui.BeginMenu("Remove Reference"))
                    {
                        foreach (string p in cellMeta.RefTypes)
                        {
                            if (ImGui.Selectable(p))
                            {
                                cellMeta.RefTypes.Remove(p);
                                if (cellMeta.RefTypes.Count == 0)
                                    cellMeta.RefTypes = null;
                                break;
                            }
                        }
                        ImGui.EndMenu();
                    }
                    if (ImGui.Selectable(cellMeta.IsBool ? "Remove bool toggle" : "Add bool toggle"))
                        cellMeta.IsBool = !cellMeta.IsBool;
                    if (cellMeta.Wiki == null && ImGui.Selectable("Add wiki..."))
                        cellMeta.Wiki = "Empty wiki...";
                    if (cellMeta.Wiki != null && ImGui.Selectable("Remove wiki"))
                        cellMeta.Wiki = null;
                }
                ImGui.EndPopup();
            }
        }
        private void PropertyRowValueContextMenu(string visualName, string VirtualRef, dynamic oldval)
        {
            if (ImGui.BeginPopupContextItem("quickMEdit"))
            {
                if (ImGui.Selectable("Edit all selected..."))
                {
                    EditorCommandQueue.AddCommand($@"param/menu/massEditRegex/selection: {visualName}: ");
                }
                if (VirtualRef != null)
                    PropertyRowVirtualRefContextItems(VirtualRef, oldval);
                if (ParamEditorScreen.EditorMode && ImGui.BeginMenu("Find rows with this value..."))
                {
                    foreach(KeyValuePair<string, PARAM> p in ParamBank.Params)
                    {
                        int v = (int)oldval;
                        PARAM.Row r = p.Value[v];
                        if (r != null && ImGui.Selectable($@"{p.Key}: {(r.Name != null ? r.Name : "null")}"))
                            EditorCommandQueue.AddCommand($@"param/select/-1/{p.Key}/{v}");
                    }
                    ImGui.EndMenu();
                }
                ImGui.EndPopup();
            }
        }
        private bool PropertyRowMetaValueContextMenu(object oldval, ref object newval, List<string> RefTypes, ParamEnum Enum)
        {
            if (RefTypes == null && Enum == null)
                return false;
            bool result = false;
            if (ImGui.BeginPopupContextItem("rowMetaValue"))
            {
                if (RefTypes != null)
                    result |= PropertyRowRefsContextItems(RefTypes, oldval, ref newval);
                if (Enum != null)
                    result |= PropertyRowEnumContextItems(Enum, oldval, ref newval);
                ImGui.EndPopup();
            }
            return result;
        }

        private bool PropertyRowRefsContextItems(List<string> reftypes, dynamic oldval, ref object newval)
        { 
            // Add Goto statements
            foreach (string rt in reftypes)
            {
                if (!ParamBank.Params.ContainsKey(rt))
                {
                    continue;
                }
                int searchVal = (int) oldval;
                ParamMetaData meta = ParamMetaData.Get(ParamBank.Params[rt].AppliedParamdef);
                if (meta != null)
                {
                    if (meta.Row0Dummy && searchVal == 0)
                        continue;
                    if (meta.OffsetSize > 0 && searchVal > 0 && ParamBank.Params[rt][(int) searchVal] == null)
                    {
                        // Test if previous row exists. In future, add param meta to determine size of offset
                        searchVal = (int) oldval - (int) oldval % meta.OffsetSize;
                    }
                }
                if (ParamBank.Params[rt][searchVal] != null)
                {   
                    if (ImGui.Selectable($@"Go to {rt}"))
                        EditorCommandQueue.AddCommand($@"param/select/-1/{rt}/{searchVal}");
                    if (ImGui.Selectable($@"Go to {rt} in new view"))
                        EditorCommandQueue.AddCommand($@"param/select/new/{rt}/{searchVal}");
                }
            }
            // Add searchbar for named editing
            ImGui.InputText("##value", ref _refContextCurrentAutoComplete, 128);
            // Unordered scanthrough search for matching param entries.
            // This should be replaced by a proper search box with a scroll and everything
            if (_refContextCurrentAutoComplete != "")
            {
                foreach (string rt in reftypes)
                {
                    int maxResultsPerRefType = 15/reftypes.Count;
                    List<PARAM.Row> rows = MassParamEditRegex.GetMatchingParamRowsByName(ParamBank.Params[rt], _refContextCurrentAutoComplete, true, false);
                    foreach (PARAM.Row r in rows)
                    {
                        if (maxResultsPerRefType <= 0)
                            break;
                        if (ImGui.Selectable(r.Name))
                        {
                            newval = (int) r.ID;
                            _refContextCurrentAutoComplete = "";
                            return true;
                        }
                        maxResultsPerRefType--;
                    }
                }
            }
            return false;
        }
        private void PropertyRowVirtualRefContextItems(string vref, object searchValue)
        {
            // Add Goto statements
            foreach (var param in ParamBank.Params)
            {
                PARAMDEF.Field foundfield = null;
                foreach (PARAMDEF.Field f in param.Value.AppliedParamdef.Fields)
                { 
                    if (FieldMetaData.Get(f).VirtualRef != null && FieldMetaData.Get(f).VirtualRef.Equals(vref))
                    {
                        foundfield = f;
                        break;
                    }
                }
                if (foundfield == null)
                    continue;
                if (ImGui.Selectable($@"Go to first in {param.Key}"))
                {   
                            Console.WriteLine($@"{foundfield.InternalName} - {searchValue}");
                            Console.WriteLine($@"{param.Key}");
                    foreach (PARAM.Row row in param.Value.Rows)
                    {
                            Console.WriteLine($@"{row.ID} - {row[foundfield.InternalName].Value}");
                        if (row[foundfield.InternalName].Value.ToString().Equals(searchValue.ToString()))
                        {
                            EditorCommandQueue.AddCommand($@"param/select/-1/{param.Key}/{row.ID}");
                            Console.WriteLine($@"param/select/-1/{param.Key}/{row.ID}");
                            break;
                        }
                    }
                }
            }
        }
        private bool PropertyRowEnumContextItems(ParamEnum en, object oldval, ref object newval)
        {
            try
            {
                foreach (KeyValuePair<string, string> option in en.values)
                {
                    if (ImGui.Selectable($"{option.Key}: {option.Value}"))
                    {
                        newval = Convert.ChangeType(option.Key, oldval.GetType());
                        return true;
                    }
                }
            }
            catch
            {

            }
            return false;
        }

        private int _fmgID = 0;
        public void PropEditorFMGBegin()
        {
            _fmgID = 0;
            ImGui.Columns(2);
            ImGui.Separator();
        }

        public void PropEditorFMG(FMG.Entry entry, string name, float boxsize)
        {
            ImGui.PushID(_fmgID);
            ImGui.AlignTextToFramePadding();
            ImGui.Text(name);
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            // ImGui.AlignTextToFramePadding();
            var typ = typeof(string);
            var oldval = entry.Text;
            bool changed = false;
            object newval = null;

            string val = (string)oldval;
            if (val == null)
            {
                val = "";
            }
            if (boxsize > 0.0f)
            {
                if (ImGui.InputTextMultiline("##value", ref val, 2000, new Vector2(-1, boxsize)))
                {
                    newval = val;
                    changed = true;
                }
            }
            else
            {
                if (ImGui.InputText("##value", ref val, 2000))
                {
                    newval = val;
                    changed = true;
                }
            }

            bool committed = ImGui.IsItemDeactivatedAfterEdit();
            UpdateProperty(entry.GetType().GetProperty("Text"), entry, newval, changed, committed);

            ImGui.NextColumn();
            ImGui.PopID();
            _fmgID++;
        }

        public void PropEditorFMGEnd()
        {
            ImGui.Columns(1);
        }

        public void OnGui(PARAM.Row selection, string id, float w, float h)
        {
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.145f, 0.145f, 0.149f, 1.0f));
            ImGui.SetNextWindowSize(new Vector2(350, h - 80), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(w - 370, 20), ImGuiCond.FirstUseEver);
            ImGui.Begin($@"Properties##{id}");
            ImGui.BeginChild("propedit");
            string _noSearchStr = null;
            PropEditorParamRow(selection, ref _noSearchStr);
            ImGui.EndChild();
            ImGui.End();
            ImGui.PopStyleColor();
        }
    }
}
