Feature: Cancelar reserva
    Como cliente
    Quiero cancelar una reserva desde la página de consulta
    Para poder liberar los medicamentos si ya no los necesito

Background:
    Given existe una reserva en estado "Pendiente" con clave pública "CLAVE-CANCEL-TEST" y fecha de expiración "2026-06-22"
    And el cliente visita la página "/reservations"
    And ingresa la clave pública "CLAVE-CANCEL-TEST" en el campo "#public-key-input"
    And hace clic en "#btn-buscar-reserva"

Scenario: Cancelación exitosa de una reserva pendiente
    Given el sistema muestra el botón "#btn-cancelar-reserva"
    When hace clic en "#btn-cancelar-reserva"
    Then el sistema debe cambiar el estado de la reserva a "Cancelled"
    And el botón "#btn-cancelar-reserva" no debe estar visible
