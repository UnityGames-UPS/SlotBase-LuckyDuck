using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using System;
using System.Security;

public class ManageLineButtons : MonoBehaviour, IPointerEnterHandler,IPointerExitHandler, IPointerUpHandler,IPointerDownHandler
{

	internal int num;
	[SerializeField] internal TMP_Text num_text;

	internal Action<int,bool> GenerateLine;
	internal Action<bool> DestroyLine;
	[SerializeField] private int index;
	[SerializeField] private SlotBehaviour slotmanager;
	[SerializeField] private PayoutCalculation payoutcalculation;


	public void OnPointerEnter(PointerEventData eventData)
	{

		//GenerateLine?.Invoke(num, false);
		// slotManager.GenerateStaticLine(num_text);
		//slotmanager.GenerateStaticLine(index);
		payoutcalculation.GeneratePayoutLines(index, false);
	}
	public void OnPointerExit(PointerEventData eventData)
	{
		DestroyLine?.Invoke(false);
		// slotManager.DestroyStaticLine();
		//slotManager.DestroyStaticLine();
		payoutcalculation.ResetLines(false);
	}
	public void OnPointerDown(PointerEventData eventData)
	{
		if (Application.platform == RuntimePlatform.WebGLPlayer && Application.isMobilePlatform)
		{
			this.gameObject.GetComponent<Button>().Select();
			// slotManager.GenerateStaticLine(num_text);
			GenerateLine?.Invoke(num,false);

		}
	}
	public void OnPointerUp(PointerEventData eventData)
	{
		if (Application.platform == RuntimePlatform.WebGLPlayer && Application.isMobilePlatform)
		{
			//Debug.Log("run on pointer up");
			// slotManager.DestroyStaticLine();
		//	DestroyLine?.Invoke(false);
            payoutcalculation.ResetLines(false);
			DOVirtual.DelayedCall(0.1f, () =>
			{
				this.gameObject.GetComponent<Button>().spriteState = default;
				EventSystem.current.SetSelectedGameObject(null);
			 });
		}
	}
}
