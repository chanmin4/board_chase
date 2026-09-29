using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Authoring window for the single Home-shop configuration asset. It exposes
/// catalog weights and the persistent stage/quest unlock tree in one place.
/// </summary>
public sealed class ShopProgressionTreeWindow : EditorWindow
{
    private const string DefaultConfigFolder = "Assets/ScriptableObject/GameScene/Shop";

    private enum ShopCatalogView
    {
        Setup,
        Products
    }

    private ShopConfigSO _config;
    private Vector2 _catalogScrollPosition;
    private Vector2 _treeScrollPosition;
    private Vector2 _nodeScrollPosition;
    private int _selectedNodeIndex = -1;
    private int _selectedProductIndex = -1;
    private int _prerequisiteToAddIndex;
    private int _productToAddIndex;
    private ShopCatalogView _catalogView;

    [MenuItem("Tools/Authoring/Shop Progression Tree")]
    // Purpose: Opens the open UI or workflow.
    private static void Open()
    {
        ShopProgressionTreeWindow window = GetWindow<ShopProgressionTreeWindow>("Shop Progression Tree");
        window.minSize = new Vector2(1180f, 640f);
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        FindDefaultConfig();
    }

    // Purpose: Responds to the gui callback.
    private void OnGUI()
    {
        AuthoringWorkspaceUi.DrawHeader(
            "Home Shop Progression Tree",
            "Owner: Economy / Progression",
            "One ShopConfigSO owns catalog, item-payment trades, offer weights, and unlock rules. Run stock resets on player death.",
            "1 Config",
            "2 Economy",
            "3 Catalog",
            "4 Unlock Tree",
            "5 Validate");

        DrawConfigSection();

        if (_config != null)
        {
            SerializedObject serialized = new SerializedObject(_config);
            serialized.Update();

            float paneHeight = Mathf.Max(420f, position.height - 178f);
            SerializedProperty nodes = serialized.FindProperty("_unlockNodes");
            SerializedProperty products = serialized.FindProperty("_products");
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(320f), GUILayout.Height(paneHeight)))
                {
                    DrawShopCatalogPane(serialized, products);
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.Height(paneHeight)))
                    DrawTreeGraphPane(nodes, products, paneHeight);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(340f), GUILayout.Height(paneHeight)))
                {
                    DrawPaneHeader(
                        HasSelectedProduct(products) ? "PRODUCT INSPECTOR" : "UNLOCK INSPECTOR",
                        HasSelectedProduct(products)
                            ? "Edit the selected shop offer, stock, probability, and item-payment requirements."
                            : "Select a graph node, then define its requirements and unlocked products.");
                    _nodeScrollPosition = EditorGUILayout.BeginScrollView(_nodeScrollPosition);
                    DrawShopContextInspector(serialized, nodes, products);
                    EditorGUILayout.EndScrollView();
                }
            }

            if (serialized.ApplyModifiedProperties())
                EditorUtility.SetDirty(_config);
        }
    }

    // Purpose: Draws the pane header authoring UI.
    private static void DrawPaneHeader(string title, string description)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.LabelField(description, EditorStyles.miniLabel);
        EditorGUILayout.Space(3f);
    }

    // Purpose: Draws the config section authoring UI.
    private void DrawConfigSection()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("1. Shop Config", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _config = (ShopConfigSO)EditorGUILayout.ObjectField(
            "Shop Config SO",
            _config,
            typeof(ShopConfigSO),
            false);

        if (EditorGUI.EndChangeCheck())
        {
            _selectedNodeIndex = -1;
            _selectedProductIndex = -1;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create Shop Config..."))
                CreateConfigAsset();

            using (new EditorGUI.DisabledScope(_config == null))
            {
                if (GUILayout.Button("Select Config"))
                {
                    Selection.activeObject = _config;
                    EditorGUIUtility.PingObject(_config);
                }
            }
        }

    }

    // Purpose: Draws the economy section authoring UI.
    private static void DrawEconomySection(SerializedObject serialized)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("2. Home Shop Economy", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serialized.FindProperty("_configId"));
        EditorGUILayout.PropertyField(serialized.FindProperty("_bulletOfferCount"));
        EditorGUILayout.PropertyField(serialized.FindProperty("_itemOfferCount"));
        EditorGUILayout.PropertyField(serialized.FindProperty("_allowDuplicateBulletOffers"));
        EditorGUILayout.PropertyField(serialized.FindProperty("_allowDuplicateItemOffers"));
        EditorGUILayout.PropertyField(serialized.FindProperty("_restockIntervalSeconds"));
        EditorGUILayout.Space(3f);
        EditorGUILayout.LabelField("PHYSICAL PAYMENT", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(serialized.FindProperty("_defaultCurrencyItem"), new GUIContent("Default Money Item"));
        EditorGUILayout.HelpBox(
            "Every catalog offer uses the item-payment window. A product with no explicit barter rows uses this Money Item at its Default Money Amount. Add explicit rows below for combinations such as Battery x2 + Weapon x1.",
            MessageType.Info);
        EditorGUILayout.PropertyField(serialized.FindProperty("_rerollCost"), new GUIContent("Reroll Cost (WIP Numeric)"));
        EditorGUILayout.PropertyField(serialized.FindProperty("_sellPriceRate"));
        EditorGUILayout.PropertyField(serialized.FindProperty("_armorRepairPriceRate"));
        EditorGUILayout.PropertyField(serialized.FindProperty("_allowReroll"));

        EditorGUILayout.Space(3f);
        EditorGUILayout.LabelField("Always Available Infection Control Service", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(serialized.FindProperty("_infectionControlRecoveryItem"));
        EditorGUILayout.HelpBox(
            "The assigned recovery item takes one Item offer slot, has unlimited stock, restores Infection Control immediately, and raises its own price after each purchase during the run.",
            MessageType.None);
    }

    // Purpose: Draws the catalog section authoring UI.
    private void DrawCatalogSection(SerializedObject serialized)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("3. Product Catalog, Trade, and Roll Weight", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "One row is one shop product. Payment is always physical items: leave explicit rows empty to use Default Money Item, or add one or more rows for barter. Weight is the appearance probability among currently unlocked products in the same Bullet or Item section.",
            MessageType.None);

        SerializedProperty products = serialized.FindProperty("_products");

        for (int i = 0; i < products.arraySize; i++)
        {
            SerializedProperty product = products.GetArrayElementAtIndex(i);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Product {i + 1}", EditorStyles.boldLabel);

                    if (GUILayout.Button("Remove", GUILayout.Width(62f)))
                    {
                        products.DeleteArrayElementAtIndex(i);
                        _selectedNodeIndex = -1;
                        break;
                    }
                }

                EditorGUILayout.PropertyField(product.FindPropertyRelative("_productId"));
                EditorGUILayout.PropertyField(product.FindPropertyRelative("_item"));
                EditorGUILayout.PropertyField(product.FindPropertyRelative("_availableByDefault"));
                EditorGUILayout.PropertyField(product.FindPropertyRelative("_offerWeight"));
                EditorGUILayout.PropertyField(product.FindPropertyRelative("_stock"));
                EditorGUILayout.PropertyField(product.FindPropertyRelative("_bundleAmount"));
                EditorGUILayout.PropertyField(product.FindPropertyRelative("_priceOverride"), new GUIContent("Default Money Amount"));
                DrawProductTradeRequirements(product);
            }
        }

        if (GUILayout.Button("Add Product"))
            AddProduct(products);
    }

    // Purpose: Draws the left asset-store-style shop catalogue and switches between global setup and product cards.
    private void DrawShopCatalogPane(SerializedObject serialized, SerializedProperty products)
    {
        DrawPaneHeader("SHOP CATALOG", "Use Setup for global shop rules, then select a product card to edit one offer at a time.");

        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            Color originalColor = GUI.backgroundColor;
            if (_catalogView == ShopCatalogView.Setup)
                GUI.backgroundColor = new Color(0.18f, 0.56f, 0.68f, 0.78f);
            if (GUILayout.Button("Shop Setup", EditorStyles.toolbarButton))
                _catalogView = ShopCatalogView.Setup;

            GUI.backgroundColor = _catalogView == ShopCatalogView.Products
                ? new Color(0.18f, 0.56f, 0.68f, 0.78f)
                : originalColor;
            if (GUILayout.Button($"Products ({products.arraySize})", EditorStyles.toolbarButton))
                _catalogView = ShopCatalogView.Products;

            GUI.backgroundColor = originalColor;
        }

        _catalogScrollPosition = EditorGUILayout.BeginScrollView(_catalogScrollPosition);
        if (_catalogView == ShopCatalogView.Setup)
            DrawEconomySection(serialized);
        else
            DrawProductCatalogBrowser(products);
        EditorGUILayout.EndScrollView();
    }

    // Purpose: Draws each shop product as a selectable card instead of exposing every product's full property tree at once.
    private void DrawProductCatalogBrowser(SerializedProperty products)
    {
        EditorGUILayout.LabelField("PRODUCT ASSETS", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField(
            "One card is one possible shop offer. Select it to edit price or barter requirements in the right inspector.",
            EditorStyles.wordWrappedMiniLabel);

        if (GUILayout.Button("+ Add Product", GUILayout.Height(24f)))
        {
            AddProduct(products);
            _selectedProductIndex = products.arraySize - 1;
            _selectedNodeIndex = -1;
        }

        EditorGUILayout.Space(4f);
        for (int index = 0; index < products.arraySize; index++)
            DrawProductCard(products, index);

        if (products.arraySize == 0)
            EditorGUILayout.HelpBox("This shop has no products yet. Add one product, then choose its ItemSO and payment in the right inspector.", MessageType.Info);
    }

    // Purpose: Renders one concise product card containing only authoring identity, stock, and roll-weight summary data.
    private void DrawProductCard(SerializedProperty products, int index)
    {
        SerializedProperty product = products.GetArrayElementAtIndex(index);
        ItemSO item = product.FindPropertyRelative("_item").objectReferenceValue as ItemSO;
        bool selected = index == _selectedProductIndex;
        Color originalColor = GUI.backgroundColor;
        if (selected)
            GUI.backgroundColor = new Color(0.16f, 0.58f, 0.70f, 0.78f);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                Texture preview = item != null && item.PreviewImage != null
                    ? item.PreviewImage.texture
                    : Texture2D.grayTexture;
                if (GUILayout.Button(new GUIContent(preview, "Select this product"), GUILayout.Width(42f), GUILayout.Height(42f)))
                {
                    _selectedProductIndex = index;
                    _selectedNodeIndex = -1;
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    string productId = product.FindPropertyRelative("_productId").stringValue;
                    EditorGUILayout.LabelField(item != null ? item.name : "Unassigned Item", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(productId) ? "Missing Product Id" : productId, EditorStyles.miniLabel);
                    int weight = product.FindPropertyRelative("_offerWeight").intValue;
                    int stock = product.FindPropertyRelative("_stock").intValue;
                    EditorGUILayout.LabelField($"Weight {weight}  |  Stock {stock}", EditorStyles.miniLabel);
                }
            }
        }

        GUI.backgroundColor = originalColor;
    }

    // Purpose: Routes the right panel to the selected product or the selected unlock node without mixing the two authoring jobs.
    private void DrawShopContextInspector(
        SerializedObject serialized,
        SerializedProperty nodes,
        SerializedProperty products)
    {
        if (HasSelectedProduct(products))
        {
            DrawSelectedProductEditor(products);
            return;
        }

        DrawNodeInspectorPane(serialized, nodes);
    }

    // Purpose: Checks whether the stored product-card selection is still valid after catalog changes.
    private bool HasSelectedProduct(SerializedProperty products)
    {
        return _selectedProductIndex >= 0 && _selectedProductIndex < products.arraySize;
    }

    // Purpose: Edits exactly one selected shop offer so long product arrays remain readable.
    private void DrawSelectedProductEditor(SerializedProperty products)
    {
        SerializedProperty product = products.GetArrayElementAtIndex(_selectedProductIndex);
        EditorGUILayout.LabelField($"Product {_selectedProductIndex + 1}", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(product.FindPropertyRelative("_productId"));
        EditorGUILayout.PropertyField(product.FindPropertyRelative("_item"));
        EditorGUILayout.PropertyField(product.FindPropertyRelative("_availableByDefault"));
        EditorGUILayout.PropertyField(product.FindPropertyRelative("_offerWeight"));
        EditorGUILayout.PropertyField(product.FindPropertyRelative("_stock"));
        EditorGUILayout.PropertyField(product.FindPropertyRelative("_bundleAmount"));
        EditorGUILayout.PropertyField(product.FindPropertyRelative("_priceOverride"), new GUIContent("Default Money Amount"));
        DrawProductTradeRequirements(product);

        EditorGUILayout.Space(6f);
        using (new EditorGUILayout.HorizontalScope())
        {
            ItemSO item = product.FindPropertyRelative("_item").objectReferenceValue as ItemSO;
            using (new EditorGUI.DisabledScope(item == null))
            {
                if (GUILayout.Button("Select Item Asset"))
                    Selection.activeObject = item;
            }

            if (GUILayout.Button("Remove Product"))
            {
                products.DeleteArrayElementAtIndex(_selectedProductIndex);
                _selectedProductIndex = Mathf.Clamp(_selectedProductIndex, -1, products.arraySize - 1);
            }
        }
    }

    // Purpose: Draws one product's optional multi-item barter requirements in a compact, author-friendly list.
    private static void DrawProductTradeRequirements(SerializedProperty product)
    {
        SerializedProperty requirements = product.FindPropertyRelative("_tradeRequirements");
        if (requirements == null)
            return;

        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField("ITEM PAYMENT / BARTER", EditorStyles.miniBoldLabel);

        if (requirements.arraySize == 0)
        {
            EditorGUILayout.HelpBox(
                "No explicit barter rows. This product uses Default Money Item × Default Money Amount.",
                MessageType.None);
        }
        else
        {
            for (int requirementIndex = 0; requirementIndex < requirements.arraySize; requirementIndex++)
            {
                SerializedProperty requirement = requirements.GetArrayElementAtIndex(requirementIndex);
                SerializedProperty item = requirement.FindPropertyRelative("_item");
                SerializedProperty amount = requirement.FindPropertyRelative("_amount");

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(item, GUIContent.none);
                    EditorGUILayout.PropertyField(amount, GUIContent.none, GUILayout.Width(64f));

                    if (GUILayout.Button("X", GUILayout.Width(24f)))
                    {
                        requirements.DeleteArrayElementAtIndex(requirementIndex);
                        break;
                    }
                }
            }
        }

        if (GUILayout.Button("+ Add Required Item"))
        {
            requirements.arraySize++;
            SerializedProperty added = requirements.GetArrayElementAtIndex(requirements.arraySize - 1);
            added.FindPropertyRelative("_amount").intValue = 1;
        }
    }

    // Purpose: Draws the tree graph pane authoring UI.
    private void DrawTreeGraphPane(SerializedProperty nodes, SerializedProperty products, float paneHeight)
    {
        DrawPaneHeader("UNLOCK FLOW", "Conditions flow forward into product unlocks. Click a node to edit it.");
        EditorGUILayout.HelpBox(
            "Top half = unlock gates. Bottom half = products granted by this node. Arrows mean prerequisite node requirements.",
            MessageType.None);

        DrawTreeCanvas(nodes, products, paneHeight - 112f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Unlock Node"))
            {
                AddUnlockNode(nodes);
                _selectedNodeIndex = nodes.arraySize - 1;
                _selectedProductIndex = -1;
            }

        }
    }

    // Purpose: Draws the node inspector pane authoring UI.
    private void DrawNodeInspectorPane(SerializedObject serialized, SerializedProperty nodes)
    {
        using (new EditorGUI.DisabledScope(_selectedNodeIndex < 0 || _selectedNodeIndex >= nodes.arraySize))
        {
            if (GUILayout.Button("Remove Selected Node"))
            {
                nodes.DeleteArrayElementAtIndex(_selectedNodeIndex);
                _selectedNodeIndex = Mathf.Clamp(_selectedNodeIndex, -1, nodes.arraySize - 1);
            }
        }

        if (_selectedNodeIndex < 0 || _selectedNodeIndex >= nodes.arraySize)
        {
            EditorGUILayout.HelpBox("Select an unlock node in the graph to edit it here.", MessageType.Info);
            return;
        }

        DrawSelectedNodeEditor(serialized, nodes.GetArrayElementAtIndex(_selectedNodeIndex), nodes);
    }

    // Purpose: Draws the tree canvas authoring UI.
    private void DrawTreeCanvas(SerializedProperty nodes, SerializedProperty products, float availableHeight)
    {
        const float nodeWidth = 235f;
        const float nodeHeight = 132f;
        const float columnGap = 42f;
        const float rowGap = 22f;
        const float minimumHeight = 240f;

        Dictionary<string, int> indexById = BuildNodeIndex(nodes);
        Dictionary<int, int> depthByIndex = new Dictionary<int, int>();
        Dictionary<int, int> rowByIndex = new Dictionary<int, int>();
        Dictionary<int, int> nextRowByDepth = new Dictionary<int, int>();
        Dictionary<int, Rect> rectByIndex = new Dictionary<int, Rect>();

        for (int i = 0; i < nodes.arraySize; i++)
        {
            int depth = ResolveNodeDepth(i, nodes, indexById, depthByIndex, new HashSet<int>());
            if (!nextRowByDepth.TryGetValue(depth, out int row))
                row = 0;

            rowByIndex[i] = row;
            nextRowByDepth[depth] = row + 1;
        }

        int maxDepth = 0;
        int maxRow = 0;

        foreach (KeyValuePair<int, int> pair in depthByIndex)
            maxDepth = Mathf.Max(maxDepth, pair.Value);
        foreach (KeyValuePair<int, int> pair in rowByIndex)
            maxRow = Mathf.Max(maxRow, pair.Value);

        float contentWidth = 24f + (maxDepth + 1) * (nodeWidth + columnGap);
        float contentHeight = Mathf.Max(minimumHeight, 22f + (maxRow + 1) * (nodeHeight + rowGap));

        _treeScrollPosition = EditorGUILayout.BeginScrollView(
            _treeScrollPosition,
            GUILayout.Height(Mathf.Max(180f, Mathf.Min(availableHeight, contentHeight + 18f))));

        Rect canvas = GUILayoutUtility.GetRect(contentWidth, contentHeight);
        EditorGUI.DrawRect(canvas, new Color(0.055f, 0.09f, 0.11f));

        for (int i = 0; i < nodes.arraySize; i++)
        {
            int depth = depthByIndex[i];
            int row = rowByIndex[i];
            rectByIndex[i] = new Rect(
                canvas.x + 12f + depth * (nodeWidth + columnGap),
                canvas.y + 12f + row * (nodeHeight + rowGap),
                nodeWidth,
                nodeHeight);
        }

        if (Event.current.type == EventType.Repaint)
        {
            Handles.BeginGUI();
            Handles.color = new Color(0.26f, 0.78f, 0.82f, 0.9f);

            for (int i = 0; i < nodes.arraySize; i++)
            {
                SerializedProperty prerequisites = nodes.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("_prerequisiteNodeIds");

                for (int prerequisiteIndex = 0; prerequisiteIndex < prerequisites.arraySize; prerequisiteIndex++)
                {
                    string id = prerequisites.GetArrayElementAtIndex(prerequisiteIndex).stringValue;

                    if (!indexById.TryGetValue(id, out int sourceIndex) ||
                        !rectByIndex.TryGetValue(sourceIndex, out Rect source) ||
                        !rectByIndex.TryGetValue(i, out Rect target))
                    {
                        continue;
                    }

                    Vector3 start = new Vector3(source.xMax, source.center.y, 0f);
                    Vector3 end = new Vector3(target.xMin, target.center.y, 0f);
                    Handles.DrawAAPolyLine(2.5f, start, end);
                    DrawArrowHead(end, Vector3.right);
                }
            }

            Handles.EndGUI();
        }

        for (int i = 0; i < nodes.arraySize; i++)
        {
            SerializedProperty node = nodes.GetArrayElementAtIndex(i);
            Rect rect = rectByIndex[i];
            bool selected = i == _selectedNodeIndex;
            EditorGUI.DrawRect(rect, selected ? new Color(0.10f, 0.34f, 0.40f) : new Color(0.08f, 0.20f, 0.24f));

            string title = node.FindPropertyRelative("_displayName").stringValue;
            if (string.IsNullOrWhiteSpace(title))
                title = node.FindPropertyRelative("_nodeId").stringValue;

            int minimumStage = node.FindPropertyRelative("_minimumHighestUnlockedStageIndex").intValue;
            int questCount = node.FindPropertyRelative("_requiredCompletedQuests").arraySize;
            SerializedProperty prerequisites = node.FindPropertyRelative("_prerequisiteNodeIds");
            SerializedProperty productIds = node.FindPropertyRelative("_unlockedProductIds");
            int prerequisiteCount = prerequisites.arraySize;
            int productCount = productIds.arraySize;
            string gateSummary = minimumStage >= 0 ? $"Stage gate: {minimumStage}" : "Stage gate: none";
            string requirementSummary = $"Requires: {prerequisiteCount} node(s), {questCount} quest(s)";
            string rewardSummary = BuildUnlockedProductPreview(productIds, products);
            EditorGUI.DrawRect(new Rect(rect.x + 6f, rect.y + 31f, rect.width - 12f, 39f), new Color(0f, 0f, 0f, 0.16f));
            EditorGUI.DrawRect(new Rect(rect.x + 6f, rect.y + 73f, rect.width - 12f, 51f), new Color(0f, 0f, 0f, 0.10f));
            GUI.Label(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, 20f), title, EditorStyles.boldLabel);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 35f, rect.width - 20f, 16f), gateSummary, EditorStyles.miniLabel);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 52f, rect.width - 20f, 16f), requirementSummary, EditorStyles.miniLabel);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 77f, rect.width - 20f, 14f), "UNLOCKS", EditorStyles.miniBoldLabel);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 94f, rect.width - 20f, 16f), rewardSummary, EditorStyles.miniLabel);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 111f, rect.width - 20f, 14f), $"{productCount} product(s)", EditorStyles.miniLabel);

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                _selectedNodeIndex = i;
                _selectedProductIndex = -1;
                GUI.FocusControl(null);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    // Purpose: Builds the unlocked product preview from the current authored data.
    private static string BuildUnlockedProductPreview(SerializedProperty productIds, SerializedProperty products)
    {
        if (productIds == null || productIds.arraySize == 0)
            return "No product reward";

        string firstId = productIds.GetArrayElementAtIndex(0).stringValue;
        string firstLabel = ResolveProductLabel(products, firstId);
        if (productIds.arraySize == 1)
            return TruncateForNode(firstLabel);

        return $"{TruncateForNode(firstLabel)} +{productIds.arraySize - 1}";
    }

    // Purpose: Performs the truncate for node workflow for this component.
    private static string TruncateForNode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Unnamed product";

        return value.Length <= 28 ? value : value.Substring(0, 25) + "...";
    }

    // Purpose: Draws the selected node editor authoring UI.
    private void DrawSelectedNodeEditor(
        SerializedObject serialized,
        SerializedProperty node,
        SerializedProperty allNodes)
    {
        EditorGUILayout.Space(5f);
        EditorGUILayout.LabelField("1. NODE IDENTITY", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(node.FindPropertyRelative("_nodeId"));
        EditorGUILayout.PropertyField(node.FindPropertyRelative("_displayName"));
        EditorGUILayout.PropertyField(node.FindPropertyRelative("_description"));
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("2. UNLOCK GATES", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(node.FindPropertyRelative("_minimumHighestUnlockedStageIndex"));
        EditorGUILayout.PropertyField(node.FindPropertyRelative("_requiredCompletedQuests"), true);

        Dictionary<string, int> nodeIndexById = BuildNodeIndex(allNodes);
        DrawNodePrerequisites(node.FindPropertyRelative("_prerequisiteNodeIds"), allNodes, nodeIndexById);
        DrawNodeProducts(node.FindPropertyRelative("_unlockedProductIds"), serialized.FindProperty("_products"));
    }

    // Purpose: Draws the node prerequisites authoring UI.
    private void DrawNodePrerequisites(
        SerializedProperty prerequisites,
        SerializedProperty allNodes,
        Dictionary<string, int> nodeIndexById)
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("3. REQUIRED UNLOCK NODES", EditorStyles.miniBoldLabel);

        List<string> labels = new List<string> { "< Select prerequisite >" };
        List<string> ids = new List<string> { string.Empty };

        for (int i = 0; i < allNodes.arraySize; i++)
        {
            if (i == _selectedNodeIndex)
                continue;

            SerializedProperty candidate = allNodes.GetArrayElementAtIndex(i);
            string id = candidate.FindPropertyRelative("_nodeId").stringValue;
            string displayName = candidate.FindPropertyRelative("_displayName").stringValue;

            if (string.IsNullOrWhiteSpace(id))
                continue;

            labels.Add(string.IsNullOrWhiteSpace(displayName) ? id : $"{displayName} ({id})");
            ids.Add(id);
        }

        _prerequisiteToAddIndex = Mathf.Clamp(_prerequisiteToAddIndex, 0, labels.Count - 1);

        using (new EditorGUILayout.HorizontalScope())
        {
            _prerequisiteToAddIndex = EditorGUILayout.Popup("Add prerequisite", _prerequisiteToAddIndex, labels.ToArray());

            using (new EditorGUI.DisabledScope(_prerequisiteToAddIndex <= 0))
            {
                if (GUILayout.Button("Add", GUILayout.Width(52f)))
                {
                    string id = ids[_prerequisiteToAddIndex];

                    if (!ContainsString(prerequisites, id) && !WouldCreateCycle(id, CurrentNodeId(allNodes)))
                    {
                        prerequisites.arraySize++;
                        prerequisites.GetArrayElementAtIndex(prerequisites.arraySize - 1).stringValue = id;
                    }
                }
            }
        }

        for (int i = 0; i < prerequisites.arraySize; i++)
        {
            string id = prerequisites.GetArrayElementAtIndex(i).stringValue;
            string label = nodeIndexById.TryGetValue(id, out int nodeIndex)
                ? allNodes.GetArrayElementAtIndex(nodeIndex).FindPropertyRelative("_displayName").stringValue
                : "Missing node";

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(label) ? id : $"{label} ({id})");

                if (GUILayout.Button("Remove", GUILayout.Width(62f)))
                {
                    prerequisites.DeleteArrayElementAtIndex(i);
                    break;
                }
            }
        }
    }

    // Purpose: Draws the node products authoring UI.
    private void DrawNodeProducts(SerializedProperty productIds, SerializedProperty products)
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("4. PRODUCT REWARDS", EditorStyles.miniBoldLabel);

        List<string> labels = new List<string> { "< Select product >" };
        List<string> ids = new List<string> { string.Empty };

        for (int i = 0; i < products.arraySize; i++)
        {
            SerializedProperty product = products.GetArrayElementAtIndex(i);
            string id = product.FindPropertyRelative("_productId").stringValue;
            ItemSO item = product.FindPropertyRelative("_item").objectReferenceValue as ItemSO;

            if (string.IsNullOrWhiteSpace(id))
                continue;

            labels.Add(item != null ? $"{item.name} ({id})" : id);
            ids.Add(id);
        }

        _productToAddIndex = Mathf.Clamp(_productToAddIndex, 0, labels.Count - 1);

        using (new EditorGUILayout.HorizontalScope())
        {
            _productToAddIndex = EditorGUILayout.Popup("Add product", _productToAddIndex, labels.ToArray());

            using (new EditorGUI.DisabledScope(_productToAddIndex <= 0))
            {
                if (GUILayout.Button("Add", GUILayout.Width(52f)))
                {
                    string id = ids[_productToAddIndex];

                    if (!ContainsString(productIds, id))
                    {
                        productIds.arraySize++;
                        productIds.GetArrayElementAtIndex(productIds.arraySize - 1).stringValue = id;
                    }
                }
            }
        }

        for (int i = 0; i < productIds.arraySize; i++)
        {
            string id = productIds.GetArrayElementAtIndex(i).stringValue;
            string label = ResolveProductLabel(products, id);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label);

                if (GUILayout.Button("Remove", GUILayout.Width(62f)))
                {
                    productIds.DeleteArrayElementAtIndex(i);
                    break;
                }
            }
        }
    }

    // Purpose: Adds the requested product to this component.
    private void AddProduct(SerializedProperty products)
    {
        products.arraySize++;
        SerializedProperty product = products.GetArrayElementAtIndex(products.arraySize - 1);
        product.FindPropertyRelative("_productId").stringValue = $"product_{products.arraySize}";
        product.FindPropertyRelative("_offerWeight").floatValue = 1f;
        product.FindPropertyRelative("_stock").intValue = 1;
        product.FindPropertyRelative("_bundleAmount").intValue = 1;
        product.FindPropertyRelative("_priceOverride").intValue = -1;
    }

    // Purpose: Adds the requested unlock node to this component.
    private void AddUnlockNode(SerializedProperty nodes)
    {
        nodes.arraySize++;
        SerializedProperty node = nodes.GetArrayElementAtIndex(nodes.arraySize - 1);
        node.FindPropertyRelative("_nodeId").stringValue = $"unlock_{nodes.arraySize}";
        node.FindPropertyRelative("_displayName").stringValue = $"Shop Unlock {nodes.arraySize}";
        node.FindPropertyRelative("_minimumHighestUnlockedStageIndex").intValue = -1;
    }

    // Purpose: Creates the config asset owned by this workflow.
    private void CreateConfigAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Home Shop Config",
            "HomeShopConfig",
            "asset",
            "Choose where to save the single Home-shop catalog and progression tree.",
            DefaultConfigFolder);

        if (string.IsNullOrWhiteSpace(path))
            return;

        _config = CreateInstance<ShopConfigSO>();
        AssetDatabase.CreateAsset(_config, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = _config;
        EditorGUIUtility.PingObject(_config);
    }

    // Purpose: Builds the node index from the current authored data.
    private static Dictionary<string, int> BuildNodeIndex(SerializedProperty nodes)
    {
        Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.Ordinal);

        for (int i = 0; i < nodes.arraySize; i++)
        {
            string id = nodes.GetArrayElementAtIndex(i).FindPropertyRelative("_nodeId").stringValue;

            if (!string.IsNullOrWhiteSpace(id) && !result.ContainsKey(id))
                result.Add(id, i);
        }

        return result;
    }

    // Purpose: Resolves the node depth needed by this component.
    private static int ResolveNodeDepth(
        int nodeIndex,
        SerializedProperty nodes,
        Dictionary<string, int> indexById,
        Dictionary<int, int> cache,
        HashSet<int> path)
    {
        if (cache.TryGetValue(nodeIndex, out int cached))
            return cached;

        if (!path.Add(nodeIndex))
            return 0;

        int depth = 0;
        SerializedProperty prerequisites = nodes.GetArrayElementAtIndex(nodeIndex)
            .FindPropertyRelative("_prerequisiteNodeIds");

        for (int i = 0; i < prerequisites.arraySize; i++)
        {
            if (indexById.TryGetValue(prerequisites.GetArrayElementAtIndex(i).stringValue, out int prerequisiteIndex))
            {
                depth = Mathf.Max(
                    depth,
                    ResolveNodeDepth(prerequisiteIndex, nodes, indexById, cache, path) + 1);
            }
        }

        path.Remove(nodeIndex);
        cache[nodeIndex] = depth;
        return depth;
    }

    // Purpose: Performs the would create cycle workflow for this component.
    private bool WouldCreateCycle(string proposedPrerequisiteId, string currentNodeId)
    {
        if (string.IsNullOrWhiteSpace(proposedPrerequisiteId) || string.IsNullOrWhiteSpace(currentNodeId))
            return false;

        SerializedObject serialized = new SerializedObject(_config);
        SerializedProperty nodes = serialized.FindProperty("_unlockNodes");
        Dictionary<string, int> indexById = BuildNodeIndex(nodes);

        if (!indexById.TryGetValue(proposedPrerequisiteId, out int startIndex))
            return false;

        return HasPrerequisitePath(startIndex, currentNodeId, nodes, indexById, new HashSet<int>());
    }

    // Purpose: Checks whether this component has prerequisite path.
    private static bool HasPrerequisitePath(
        int index,
        string targetNodeId,
        SerializedProperty nodes,
        Dictionary<string, int> indexById,
        HashSet<int> visited)
    {
        if (!visited.Add(index))
            return false;

        SerializedProperty prerequisites = nodes.GetArrayElementAtIndex(index)
            .FindPropertyRelative("_prerequisiteNodeIds");

        for (int i = 0; i < prerequisites.arraySize; i++)
        {
            string id = prerequisites.GetArrayElementAtIndex(i).stringValue;

            if (id == targetNodeId)
                return true;

            if (indexById.TryGetValue(id, out int nextIndex) &&
                HasPrerequisitePath(nextIndex, targetNodeId, nodes, indexById, visited))
            {
                return true;
            }
        }

        return false;
    }

    // Purpose: Performs the current node id workflow for this component.
    private string CurrentNodeId(SerializedProperty nodes)
    {
        return _selectedNodeIndex >= 0 && _selectedNodeIndex < nodes.arraySize
            ? nodes.GetArrayElementAtIndex(_selectedNodeIndex).FindPropertyRelative("_nodeId").stringValue
            : string.Empty;
    }

    // Purpose: Performs the contains string workflow for this component.
    private static bool ContainsString(SerializedProperty values, string value)
    {
        for (int i = 0; i < values.arraySize; i++)
        {
            if (values.GetArrayElementAtIndex(i).stringValue == value)
                return true;
        }

        return false;
    }

    // Purpose: Resolves the product label needed by this component.
    private static string ResolveProductLabel(SerializedProperty products, string id)
    {
        for (int i = 0; i < products.arraySize; i++)
        {
            SerializedProperty product = products.GetArrayElementAtIndex(i);

            if (product.FindPropertyRelative("_productId").stringValue != id)
                continue;

            ItemSO item = product.FindPropertyRelative("_item").objectReferenceValue as ItemSO;
            return item != null ? $"{item.name} ({id})" : id;
        }

        return $"Missing product ({id})";
    }

    // Purpose: Draws the arrow head authoring UI.
    private static void DrawArrowHead(Vector3 end, Vector3 direction)
    {
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f);
        Vector3[] arrow =
        {
            end,
            end - direction * 9f + perpendicular * 4f,
            end - direction * 9f - perpendicular * 4f
        };
        Handles.DrawAAConvexPolygon(arrow);
    }

    // Purpose: Finds the requested default config.
    private void FindDefaultConfig()
    {
        if (_config != null)
            return;

        string[] guids = AssetDatabase.FindAssets("t:ShopConfigSO");

        if (guids.Length > 0)
            _config = AssetDatabase.LoadAssetAtPath<ShopConfigSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }
}

#pragma warning restore CS0618
