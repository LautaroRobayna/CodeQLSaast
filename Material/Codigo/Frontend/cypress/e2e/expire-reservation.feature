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
    Then solo debe aparecer la reserva con código "RES-EXP-002"

  Scenario: Una reserva confirmada se expira automáticamente
    Given existe una reserva confirmada creada hace 31 días con clave pública "CONF-EXP-001"
    When el cliente busca su reserva con clave pública "CONF-EXP-001"
    Then el sistema debe mostrar la reserva como "Expired"

  Scenario: Una reserva cancelada no se expira automáticamente
    Given existe una reserva cancelada creada hace 31 días con clave pública "CANC-EXP-001"
    When el cliente busca su reserva con clave pública "CANC-EXP-001"
    Then el sistema debe mostrar la reserva como "Cancelled"

  Scenario: Cancelación bloqueada si faltan menos de 5 días para la expiración
    Given existe una reserva pendiente creada hace 27 días con clave pública "CLOSE-TO-EXPIRE"
    When el cliente busca su reserva con clave pública "CLOSE-TO-EXPIRE"
    Then el botón "#btn-cancelar-reserva" no debe estar visible
