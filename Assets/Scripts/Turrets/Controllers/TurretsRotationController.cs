using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class TurretsRotationController : NetworkBehaviour
{
    // Yatay (Y ekseni) dönme bileşeni (Genellikle kulenin alt tabanı)
    [Header("Dönme Bileşenleri")]
    [SerializeField] Transform returnY;
    // Dikey (X ekseni) dönme bileşeni (Genellikle top namlusu/silah kısmı)
    [SerializeField] Transform returnX;

    [Header("Dönme Ayarları")]
    [SerializeField] private float rotationSpeed = 5f; // Yumuşak dönme hızı

    // Mevcut düşman hedefini tutar
    private Transform currentEnemyTarget;

    // Turret bileşenleri (NavMeshAgent artık kule için kullanılmıyor)
    // private NavMeshAgent navMesh; // Kaldırıldı
    // private Transform baseTarget; // Kaldırıldı

    public override void OnNetworkSpawn()
    {
        Debug.Log("[SERVER/Rotation] OnNetworkSpawn çağrıldı.");
        if (!IsServer)
        {
            this.enabled = false;
            Debug.Log("[SERVER/Rotation] Client: Script kapatıldı.");
            return;
        }
        Debug.Log("[SERVER/Rotation] Sunucuda çalışıyor.");

        // Bileşenlerin atanıp atanmadığını kontrol et
        if (returnY == null || returnX == null)
        {
            Debug.LogError("[SERVER/Rotation] returnY veya returnX Transform'ları atanmamış!");
            this.enabled = false; // Dönme yapamayacağı için kapat
        }
        else
        {
            Debug.Log("[SERVER/Rotation] returnY ve returnX başarıyla atanmış görünüyor.");
        }
    }

    private void Update()
    {
        // Update her kare çağrıldığından, logu sadece önemli durumlarda koymak daha iyidir.
        if (!IsServer) return; // Sunucu değilse dur

        if (currentEnemyTarget == null)
        {
            // Debug.Log("[SERVER/Rotation] Update: Hedef yok, dönme işlemi yapılmıyor.");
            return; // Hedef yoksa dur
        }

        // Debug.Log("[SERVER/Rotation] Update: Hedef mevcut, dönme metodları çağrılıyor.");
        // Her iki eksen dönme işlemini ayrı metodlarda çalıştır
        RotateYAxis();
        RotateXAxis();
    }

    /// <summary>
    /// Yatay (Y ekseni) dönme bileşenini hedefe doğru döndürür.
    /// Kule alt tabanının dönüşünü kontrol eder.
    /// </summary>
    private void RotateYAxis()
    {
        // Debug.Log("[SERVER/Rotation] RotateYAxis çağrıldı.");
        // 1. Hedefin yüksekliğini, dönen parçanın yüksekliğiyle eşitliyoruz (Sadece yatay dönme)
        Vector3 targetPositionFlat = currentEnemyTarget.position;
        targetPositionFlat.y = returnY.position.y;

        // 2. Hedefe bakmak için gerekli rotasyonu hesapla
        Vector3 direction = targetPositionFlat - returnY.position;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // 3. Yumuşak bir şekilde hedefe doğru dön (Slerp)
            returnY.rotation = Quaternion.Slerp(
                returnY.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );
            // Debug.Log("[SERVER/Rotation] returnY döndürülüyor.");
        }
        // else Debug.Log("[SERVER/Rotation] returnY konumu hedeften farksız (Sıfır vektör).");
    }

    /// <summary>
    /// Dikey (X ekseni) dönme bileşenini hedefe doğru döndürür.
    /// Namlu/silah kısmının yukarı-aşağı hareketini kontrol eder.
    /// </summary>
    private void RotateXAxis()
    {
        // Debug.Log("[SERVER/Rotation] RotateXAxis çağrıldı.");
        // 1. Hedefin pozisyonundan namlu pozisyonuna giden yönü hesapla
        Vector3 direction = currentEnemyTarget.position - returnX.position;

        // 2. Hedefe bakmak için gerekli rotasyonu hesapla
        // Quaternion.LookRotation(direction) bize hem Y hem X rotasyonu verir.
        // Biz sadece X eksenindeki eğimi (yukarı-aşağı) istiyoruz.
        Quaternion fullTargetRotation = Quaternion.LookRotation(direction);

        // 3. Hedef rotasyonun Euler açılarını al
        Vector3 targetEuler = fullTargetRotation.eulerAngles;

        // 4. Namlunun X rotasyonunu (yukarı-aşağı) hedefin X rotasyonu olarak al
        // Y ve Z rotasyonlarını mevcut değerlerinde bırak (Çünkü Y'yi zaten returnY kontrol ediyor)
        Quaternion finalRotation = Quaternion.Euler(
            targetEuler.x, // Hedefin X açısı
            returnX.rotation.eulerAngles.y, // returnX'in mevcut Y açısı (değişmemeli)
            returnX.rotation.eulerAngles.z  // returnX'in mevcut Z açısı (değişmemeli)
        );

        // 5. Yumuşak bir şekilde hedefe doğru dön (Slerp)
        returnX.rotation = Quaternion.Slerp(
            returnX.rotation,
            finalRotation,
            Time.deltaTime * rotationSpeed
        );
        // Debug.Log("[SERVER/Rotation] returnX döndürülüyor.");
    }


    /// <summary>
    /// Bir düşman hedefini (TargetDetector'dan gelen) ayarlar.
    /// </summary>
    public void GiveMeNewTarget(Transform newTarget)
    {
        Debug.Log("[SERVER/Rotation] GiveMeNewTarget çağrıldı.");
        if (!IsServer) return; // Sadece Sunucuda çalışır

        currentEnemyTarget = newTarget;

        if (currentEnemyTarget != null)
        {
            Debug.Log($"[SERVER/Rotation] Yeni DÜŞMAN hedefi alındı: ({currentEnemyTarget.name}).");
        }
        else
        {
            Debug.Log("[SERVER/Rotation] Düşman hedefi temizlendi. Kule boşa çıkacak.");
        }
    }
}
