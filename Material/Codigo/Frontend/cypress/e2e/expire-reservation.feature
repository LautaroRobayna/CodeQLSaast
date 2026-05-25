Feature: Expiración de reservas
    Como cliente
    Quiero que las reservas pendientes expiren automáticamente después de 30 días
    Para liberar el stock de medicamentos no retirados

Scenario: Expiración automática de una reserva pendiente después de 30 días
    Given existe una reserva pendiente creada hace 31 días con clave pública "CLAVE-EXPIRADA-TEST"
    And el cliente visita la página "/reservations"
    And ingresa la clave pública "CLAVE-EXPIRADA-TEST" en el campo "#public-key-input"
    And busca la reserva
    Then el sistema debe mostrar la reserva como "Expired"
    And el sistema debe mostrar el mensaje "Reserva expirada. El stock fue liberado."

  Scenario: Empleado lista reservas pendientes y las vencidas ya no aparecen
    Given existe una reserva pendiente creada hace 31 días con clave pública "EXP-ALL-001"
    And existe una reserva pendiente creada hace 5 días con clave pública "EXP-ALL-002"
    When el empleado solicita todas las reservas pendientes
    Then solo debe aparecer la reserva "EXP-ALL-002"
