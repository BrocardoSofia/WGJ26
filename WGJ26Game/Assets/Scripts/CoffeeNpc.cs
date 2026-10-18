using UnityEngine;

[DisallowMultipleComponent]
public sealed class CoffeeNpc : MonoBehaviour
{
    [SerializeField, Min(0), InspectorName("Cafés entregados")]
    private int coffeeAmount = 1;

    [SerializeField, InspectorName("Destino al rechazar")]
    private Vector3 rejectionDestination;

    [SerializeField, Min(0f), InspectorName("Velocidad al retirarse")]
    private float rejectionMoveSpeed = 1f;

    [SerializeField, Min(0f), InspectorName("Espera antes de escuchar No")]
    private float rejectionListeningDelay = 1f;

    [Header("Audio de rechazo")]
    [SerializeField, InspectorName("Audio cuando recibe un No")]
    private AudioClip rejectionAudio;

    [SerializeField, Range(0f, 2f), InspectorName("Volumen individual de rechazo")]
    private float rejectionAudioVolume = 1f;

    private bool movingToRejectionDestination;
    private bool insidePlayerCallRange;
    private float enteredCallRangeTime;

    public bool CanHearRejection =>
        insidePlayerCallRange && Time.time >= enteredCallRangeTime + rejectionListeningDelay;

    public void SetInsidePlayerCallRange(bool inside)
    {
        if (inside == insidePlayerCallRange)
            return;

        insidePlayerCallRange = inside;
        if (inside)
            enteredCallRangeTime = Time.time;
    }

    public void CompleteInteraction(PlayerCoffeeInventory inventory)
    {
        if (inventory != null)
            inventory.AddCoffee(coffeeAmount);
    }

    public void Reject()
    {
        NpcInteractionCircle interactionCircle = GetComponent<NpcInteractionCircle>();
        if (interactionCircle != null)
            interactionCircle.PlayPriorityAudio(rejectionAudio, rejectionAudioVolume);

        CameraFacingBillboard billboard = GetComponent<CameraFacingBillboard>();
        if (billboard != null)
            billboard.FaceLeftForDeparture();

        movingToRejectionDestination = true;
    }

    private void Update()
    {
        if (!movingToRejectionDestination || rejectionMoveSpeed <= 0f)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            rejectionDestination,
            rejectionMoveSpeed * Time.deltaTime);

        if ((transform.position - rejectionDestination).sqrMagnitude <= 0.000001f)
            movingToRejectionDestination = false;
    }

    private void OnValidate()
    {
        coffeeAmount = Mathf.Max(0, coffeeAmount);
        rejectionMoveSpeed = Mathf.Max(0f, rejectionMoveSpeed);
        rejectionListeningDelay = Mathf.Max(0f, rejectionListeningDelay);
    }
}
