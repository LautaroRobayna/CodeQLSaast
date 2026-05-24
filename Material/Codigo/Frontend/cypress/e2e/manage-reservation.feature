Feature: Gestionar reserva
    Como cliente
    Quiero consultar el estado de mi reserva ingresando mi clave pública
    Para ver los detalles y saber si necesito presentar alguna receta

Background:
    Given que el sistema tiene cargada la farmacia "Farmacia Central"
    And la "Farmacia Central" tiene el medicamento "Amoxicilina 500mg" que requiere receta con stock de 3 unidades
    And existe una reserva en estado "Pendiente" para el email "carlos@example.com" en "Farmacia Central" con los siguientes medicamentos:
      | Medicamento       | Cantidad |
      | Amoxicilina 500mg | 2        |

Scenario: Visualización de una reserva en estado "Pendiente" con receta faltante
    Given el cliente visita la página "/reservations"
    When ingresa la clave pública "CLAVE-PUBLICA-TEST" en el campo "#public-key-input"
    And hace clic en "#btn-buscar-reserva"
    Then el sistema debe mostrar la reserva en estado "Pendiente"
    And debe mostrar el aviso "Receta faltante" para el medicamento "Amoxicilina 500mg"

Scenario: Visualización de una reserva en estado "Confirmada" lista para retiro
    Given existe una reserva en estado "Confirmada" con clave pública "CLAVE-CONFIRMADA-TEST" y fecha de expiración "2026-06-15"
    And el cliente visita la página "/reservations"
    When ingresa la clave pública "CLAVE-CONFIRMADA-TEST" en el campo "#public-key-input"
    And hace clic en "#btn-buscar-reserva"
    Then el sistema debe mostrar la reserva en estado "Confirmada"
    And debe mostrar el mensaje "Lista para retiro en mostrador"
    And debe mostrar la fecha de expiración "2026-06-15"

Scenario: Visualización de una reserva finalizada en estado "Expirada"
    Given existe una reserva en estado "Expirada" con clave pública "CLAVE-EXPIRADA-TEST" y fecha de expiración "2026-05-01"
    And el cliente visita la página "/reservations"
    When ingresa la clave pública "CLAVE-EXPIRADA-TEST" en el campo "#public-key-input"
    And hace clic en "#btn-buscar-reserva"
    Then el sistema debe mostrar la reserva en estado "Expirada"
    And debe mostrar el mensaje "Reserva expirada. El stock fue liberado."