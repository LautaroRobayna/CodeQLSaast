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
