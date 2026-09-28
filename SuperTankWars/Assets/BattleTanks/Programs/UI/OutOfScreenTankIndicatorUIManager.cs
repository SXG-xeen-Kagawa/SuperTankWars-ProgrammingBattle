using UnityEngine;

namespace SXG2025
{
    public class OutOfScreenTankIndicatorUIManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera m_mainCamera = null;
        [SerializeField] private RectTransform m_canvasRectTransform = null;
        [SerializeField] private TankIconRenderCameraManager m_tankIconRenderCameraManager = null;
        [SerializeField] private OutOfScreenTankIndicatorItem m_outOfScreenTankPrefab = null;

        [Header("Target")]
        [SerializeField] private Transform m_stageCenterTr = null;

        [Header("Screen Clamp")]
        [SerializeField] private float m_screenEdgePadding = 90f;
        [SerializeField] private bool m_hideWhenTargetIsInFrontButInsideScreen = true;

        [Header("Rounded Corner")]
        [SerializeField] private float m_cornerRadius = 64f;


        private OutOfScreenTankIndicatorItem[] m_indicatorItems = new OutOfScreenTankIndicatorItem[GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE];


        private void Awake()
        {
            for (int i = 0; i < m_indicatorItems.Length; ++i)
            {
                m_indicatorItems[i] = Instantiate(m_outOfScreenTankPrefab, transform);
            }
        }

        private void LateUpdate()
        {
            if (m_mainCamera == null || m_canvasRectTransform == null || m_tankIconRenderCameraManager == null)
            {
                return;
            }

            for (int i = 0; i < GameConstants.MAX_PLAYER_COUNT_IN_ONE_BATTLE; i++)
            {
                UpdateIndicator(i);
            }
        }

        private void UpdateIndicator(int playerIndex)
        {
            var item = GetIndicatorItem(playerIndex);
            if (item == null)
            {
                return;
            }

            Transform targetTr = m_tankIconRenderCameraManager.GetTarget(playerIndex);
            if (targetTr == null)
            {
                item.SetActive(false);
                m_tankIconRenderCameraManager.StopRendering(playerIndex);
                return;
            }

            Vector3 worldPos = targetTr.position;
            Vector3 viewportPos = m_mainCamera.WorldToViewportPoint(worldPos);

            bool isBehindCamera = viewportPos.z < 0f;
            bool isOutsideScreen =
                viewportPos.x < 0f || viewportPos.x > 1f ||
                viewportPos.y < 0f || viewportPos.y > 1f;

            bool shouldShow = isBehindCamera || isOutsideScreen;

            if (!shouldShow && m_hideWhenTargetIsInFrontButInsideScreen)
            {
                item.SetActive(false);
                m_tankIconRenderCameraManager.StopRendering(playerIndex);
                return;
            }

            item.SetActive(true);
            m_tankIconRenderCameraManager.StartRendering(playerIndex);
            item.SetTexture(m_tankIconRenderCameraManager.GetTexture(playerIndex));

            Vector2 screenPos = CalcClampedScreenPosition(worldPos, isBehindCamera);
            Vector2 anchoredPos = ScreenToCanvasAnchoredPosition(screenPos);
            item.SetScreenPosition(anchoredPos);

            float arrowAngle = CalcArrowAngle(screenPos);
            item.SetArrowAngle(arrowAngle);

            UpdateDistanceAndHeight(item, targetTr);
        }

        private Vector2 CalcClampedScreenPosition(Vector3 worldPos, bool isBehindCamera)
        {
            Vector3 screenPos3D = m_mainCamera.WorldToScreenPoint(worldPos);

            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 rawPos = new Vector2(screenPos3D.x, screenPos3D.y);

            Vector2 dir = rawPos - screenCenter;

            if (isBehindCamera)
            {
                dir = -dir;
            }

            if (dir.sqrMagnitude <= Mathf.Epsilon)
            {
                dir = Vector2.up;
            }

            dir.Normalize();

            Rect safeRect = GetSafeRect();

            Vector2 edgePos = IntersectRayToScreenEdge(screenCenter, dir, safeRect);
            return AdjustPositionToRoundedCorner(edgePos, safeRect);
        }

        private Rect GetSafeRect()
        {
            return new Rect(
                m_screenEdgePadding,
                m_screenEdgePadding,
                Screen.width - m_screenEdgePadding * 2f,
                Screen.height - m_screenEdgePadding * 2f);
        }

        private Vector2 IntersectRayToScreenEdge(Vector2 origin, Vector2 dir, Rect rect)
        {
            float minT = float.MaxValue;
            Vector2 result = origin;

            if (Mathf.Abs(dir.x) > 0.0001f)
            {
                float tLeft = (rect.xMin - origin.x) / dir.x;
                float yLeft = origin.y + dir.y * tLeft;
                if (tLeft > 0f && yLeft >= rect.yMin && yLeft <= rect.yMax && tLeft < minT)
                {
                    minT = tLeft;
                    result = new Vector2(rect.xMin, yLeft);
                }

                float tRight = (rect.xMax - origin.x) / dir.x;
                float yRight = origin.y + dir.y * tRight;
                if (tRight > 0f && yRight >= rect.yMin && yRight <= rect.yMax && tRight < minT)
                {
                    minT = tRight;
                    result = new Vector2(rect.xMax, yRight);
                }
            }

            if (Mathf.Abs(dir.y) > 0.0001f)
            {
                float tBottom = (rect.yMin - origin.y) / dir.y;
                float xBottom = origin.x + dir.x * tBottom;
                if (tBottom > 0f && xBottom >= rect.xMin && xBottom <= rect.xMax && tBottom < minT)
                {
                    minT = tBottom;
                    result = new Vector2(xBottom, rect.yMin);
                }

                float tTop = (rect.yMax - origin.y) / dir.y;
                float xTop = origin.x + dir.x * tTop;
                if (tTop > 0f && xTop >= rect.xMin && xTop <= rect.xMax && tTop < minT)
                {
                    minT = tTop;
                    result = new Vector2(xTop, rect.yMax);
                }
            }

            return result;
        }

        private Vector2 AdjustPositionToRoundedCorner(Vector2 pos, Rect rect)
        {
            float r = Mathf.Max(1f, m_cornerRadius);

            // 左上
            if (pos.x < rect.xMin + r && pos.y > rect.yMax - r)
            {
                Vector2 center = new Vector2(rect.xMin + r, rect.yMax - r);
                return ProjectToArc(pos, center, r, 90f, 180f);
            }

            // 右上
            if (pos.x > rect.xMax - r && pos.y > rect.yMax - r)
            {
                Vector2 center = new Vector2(rect.xMax - r, rect.yMax - r);
                return ProjectToArc(pos, center, r, 0f, 90f);
            }

            // 左下
            if (pos.x < rect.xMin + r && pos.y < rect.yMin + r)
            {
                Vector2 center = new Vector2(rect.xMin + r, rect.yMin + r);
                return ProjectToArc(pos, center, r, 180f, 270f);
            }

            // 右下
            if (pos.x > rect.xMax - r && pos.y < rect.yMin + r)
            {
                Vector2 center = new Vector2(rect.xMax - r, rect.yMin + r);
                return ProjectToArc(pos, center, r, 270f, 360f);
            }

            return pos;
        }

        private Vector2 ProjectToArc(Vector2 pos, Vector2 center, float radius, float minAngleDeg, float maxAngleDeg)
        {
            Vector2 dir = pos - center;
            if (dir.sqrMagnitude <= Mathf.Epsilon)
            {
                float midAngleRad = ((minAngleDeg + maxAngleDeg) * 0.5f) * Mathf.Deg2Rad;
                dir = new Vector2(Mathf.Cos(midAngleRad), Mathf.Sin(midAngleRad));
            }
            else
            {
                dir.Normalize();
            }

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (angle < 0f)
            {
                angle += 360f;
            }

            angle = Mathf.Clamp(angle, minAngleDeg, maxAngleDeg);

            float rad = angle * Mathf.Deg2Rad;
            return center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
        }

        private Vector2 ScreenToCanvasAnchoredPosition(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_canvasRectTransform,
                screenPos,
                null,
                out Vector2 localPoint);

            return localPoint;
        }

        private float CalcArrowAngle(Vector2 screenPos)
        {
            Rect safeRect = GetSafeRect();
            float r = Mathf.Max(1f, m_cornerRadius);

            // 左上
            if (screenPos.x < safeRect.xMin + r && screenPos.y > safeRect.yMax - r)
            {
                Vector2 center = new Vector2(safeRect.xMin + r, safeRect.yMax - r);
                return CalcArcAngle(screenPos, center, 90f, 180f);
            }

            // 右上
            if (screenPos.x > safeRect.xMax - r && screenPos.y > safeRect.yMax - r)
            {
                Vector2 center = new Vector2(safeRect.xMax - r, safeRect.yMax - r);
                return CalcArcAngle(screenPos, center, 0f, 90f);
            }

            // 左下
            if (screenPos.x < safeRect.xMin + r && screenPos.y < safeRect.yMin + r)
            {
                Vector2 center = new Vector2(safeRect.xMin + r, safeRect.yMin + r);
                return CalcArcAngle(screenPos, center, 180f, 270f);
            }

            // 右下
            if (screenPos.x > safeRect.xMax - r && screenPos.y < safeRect.yMin + r)
            {
                Vector2 center = new Vector2(safeRect.xMax - r, safeRect.yMin + r);
                return CalcArcAngle(screenPos, center, 270f, 360f);
            }

            float leftDist = Mathf.Abs(screenPos.x - safeRect.xMin);
            float rightDist = Mathf.Abs(screenPos.x - safeRect.xMax);
            float bottomDist = Mathf.Abs(screenPos.y - safeRect.yMin);
            float topDist = Mathf.Abs(screenPos.y - safeRect.yMax);

            float minDist = Mathf.Min(leftDist, rightDist, bottomDist, topDist);

            if (minDist == leftDist)
            {
                return 180f;
            }
            if (minDist == rightDist)
            {
                return 0f;
            }
            if (minDist == topDist)
            {
                return 90f;
            }

            return 270f;
        }

        private float CalcArcAngle(Vector2 pos, Vector2 center, float minAngleDeg, float maxAngleDeg)
        {
            Vector2 dir = pos - center;
            if (dir.sqrMagnitude <= Mathf.Epsilon)
            {
                return (minAngleDeg + maxAngleDeg) * 0.5f;
            }

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (angle < 0f)
            {
                angle += 360f;
            }

            return Mathf.Clamp(angle, minAngleDeg, maxAngleDeg);
        }

        private void UpdateDistanceAndHeight(OutOfScreenTankIndicatorItem item, Transform targetTr)
        {
            Vector3 centerPos = m_stageCenterTr != null ? m_stageCenterTr.position : Vector3.zero;
            Vector3 diff = targetTr.position - centerPos;

            float horizontalDistance = new Vector2(diff.x, diff.z).magnitude;
            float height = diff.y;

            item.SetDistanceAndHeight(horizontalDistance, height);
        }

        private OutOfScreenTankIndicatorItem GetIndicatorItem(int playerIndex)
        {
            if (m_indicatorItems == null || playerIndex < 0 || playerIndex >= m_indicatorItems.Length)
            {
                return null;
            }

            return m_indicatorItems[playerIndex];
        }

        public void SetTeamColors(Color[] teamColors)
        {
            for (int i = 0; i < m_indicatorItems.Length; ++i)
            {
                m_indicatorItems[i].SetTeamColor(teamColors[i]);
            }
        }
    }
}