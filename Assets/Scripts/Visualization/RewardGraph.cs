using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RewardGraph : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform graphContainer;
    [SerializeField] private Sprite circleSprite;

    [Header("Settings")]
    [SerializeField] private int maxDataPoints = 50;
    [SerializeField] private Color lineColor = Color.green;

    private List<float> rewardHistory = new List<float>();
    private List<GameObject> dataPoints = new List<GameObject>();

    public void AddDataPoint(float reward)
    {
        rewardHistory.Add(reward);

        // Keep only recent data
        if (rewardHistory.Count > maxDataPoints)
        {
            rewardHistory.RemoveAt(0);
        }

        UpdateGraph();
    }

    void UpdateGraph()
    {
        // Clear old points
        foreach (GameObject point in dataPoints)
        {
            Destroy(point);
        }
        dataPoints.Clear();

        if (rewardHistory.Count < 2) return;

        // Find min/max for scaling
        float minReward = Mathf.Min(rewardHistory.ToArray());
        float maxReward = Mathf.Max(rewardHistory.ToArray());
        float range = maxReward - minReward;

        if (range < 0.1f) range = 1f; // Avoid division by zero

        float width = graphContainer.rect.width;
        float height = graphContainer.rect.height;
        float spacing = width / (maxDataPoints - 1);

        // Create line points
        for (int i = 0; i < rewardHistory.Count; i++)
        {
            float xPos = i * spacing;
            float normalizedValue = (rewardHistory[i] - minReward) / range;
            float yPos = normalizedValue * height;

            GameObject point = CreatePoint(new Vector2(xPos, yPos));
            dataPoints.Add(point);

            // Connect with line to previous point
            if (i > 0)
            {
                GameObject line = CreateLine(
                    dataPoints[i - 1].GetComponent<RectTransform>().anchoredPosition,
                    point.GetComponent<RectTransform>().anchoredPosition
                );
                dataPoints.Add(line);
            }
        }
    }

    GameObject CreatePoint(Vector2 position)
    {
        GameObject point = new GameObject("Point", typeof(Image));
        point.transform.SetParent(graphContainer, false);

        point.GetComponent<Image>().sprite = circleSprite;
        point.GetComponent<Image>().color = lineColor;

        RectTransform rectTransform = point.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(8, 8);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);

        return point;
    }

    GameObject CreateLine(Vector2 pointA, Vector2 pointB)
    {
        GameObject line = new GameObject("Line", typeof(Image));
        line.transform.SetParent(graphContainer, false);
        line.GetComponent<Image>().color = lineColor;

        RectTransform rectTransform = line.GetComponent<RectTransform>();

        Vector2 dir = (pointB - pointA).normalized;
        float distance = Vector2.Distance(pointA, pointB);

        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.sizeDelta = new Vector2(distance, 2f);
        rectTransform.anchoredPosition = pointA + dir * distance * 0.5f;
        rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        return line;
    }

    public void ClearGraph()
    {
        rewardHistory.Clear();
        foreach (GameObject point in dataPoints)
        {
            Destroy(point);
        }
        dataPoints.Clear();
    }
}