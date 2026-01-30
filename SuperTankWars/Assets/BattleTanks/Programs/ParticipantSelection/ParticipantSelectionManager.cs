using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SXG2025
{
    public class ParticipantSelectionManager : MonoBehaviour
    {
        [SerializeField]
        ParticipantList m_participantList = null;

        [SerializeField]
        ParticipantSelectionController m_controller = null;

        [SerializeField]
        ParticipantSelectionScrollView m_scrollView = null;

        [SerializeField]
        Button[] m_decideButtons = null;

        private IEnumerator Start()
        {
            // 選択中の挑戦者
            m_controller.UpdateSelectionNamePlate(0);

            // 参加者一覧
            m_scrollView.Setup(m_participantList.m_comPlayers.ToArray(), UpdateParticipant);

            // 決定ボタン操作
            foreach (var button in m_decideButtons)
            {
                button.onClick.AddListener(
                    () =>
                    {
                        StartCoroutine(CoGoToGame());
                    });
                button.interactable = false;
            }

            // フェードイン 
            FadeCanvas.Instance.FadeIn();
            // フェードイン２
            if (PromoCardsInsert.Check)
            {
                PromoCardsInsert.Instance.FadeIn();
            }
            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator CoGoToGame()
        {
            // フェードアウト
            FadeCanvas.Instance.FadeOut();
            yield return new WaitForSeconds(0.5f);

            // シーン遷移 
            SceneManager.LoadSceneAsync("Game");
        }

        private void UpdateParticipant(int scrollItemIndex)
        {
            // 現在選択中の挑戦者の情報をセット
            var participant = m_participantList.m_comPlayers[scrollItemIndex];
            m_controller.SetCurrentNamePlateData(participant.Organization, participant.YourName, participant.FaceImage);

            GameDataHolder.Instance.ParticipantIndexes[m_controller.m_currentIndex] = scrollItemIndex;

            // 次の挑戦者を選択
            m_controller.UpdateSelectionNamePlate(m_controller.m_currentIndex + 1);

            // 全ての挑戦者を選択済みなら決定ボタンを押せる
            if (m_controller.IsSetAllNamePlateData())
            {
                foreach (var button in m_decideButtons)
                {
                    button.interactable = true;
                }
            }
        }
    }
}
