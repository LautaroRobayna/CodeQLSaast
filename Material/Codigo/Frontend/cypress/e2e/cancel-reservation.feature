Feature: Cancelar reserva
    Como cliente
    Quiero cancelar una reserva desde la página de consulta
    Para poder liberar los medicamentos si ya no los necesito

Background:
    Given el cliente visita la página "/reservations"

Scenario: Cancelación exitosa de una reserva pendiente
    Given existe una reserva en estado "Pendiente" con clave pública "CLAVE-CANCEL-TEST" y fecha de expiración "2026-06-22"
    And ingresa la clave pública "CLAVE-CANCEL-TEST" en el campo "#public-key-input"
    And hace clic en "#btn-buscar-reserva"
    And el sistema muestra el botón "#btn-cancelar-reserva"
    When hace clic en "#btn-cancelar-reserva"
    Then el sistema debe cambiar el estado de la reserva a "Cancelled"
    And el botón "#btn-cancelar-reserva" no debe estar visible

Scenario: Cancelación de una reserva confirmada
    Given existe una reserva en estado "Confirmada" con clave pública "CLAVE-CANCEL-CONFIRMADA" y fecha de expiración "2026-07-15"
    And ingresa la clave pública "CLAVE-CANCEL-CONFIRMADA" en el campo "#public-key-input"
    And hace clic en "#btn-buscar-reserva"
    And el sistema muestra el botón "#btn-cancelar-reserva"
    When hace clic en "#btn-cancelar-reserva"
    Then el sistema debe cambiar el estado de la reserva a "Cancelled"
    And el botón "#btn-cancelar-reserva" no debe estar visible

Scenario: Cancelación no permitida de una reserva ya cancelada
    Given existe una reserva en estado "Cancelled" con clave pública "CLAVE-CANCEL-YA-CANCELADA" y fecha de expiración "2026-06-22"
    And ingresa la clave pública "CLAVE-CANCEL-YA-CANCELADA" en el campo "#public-key-input"
    And hace clic en "#btn-buscar-reserva"
    Then el botón "#btn-cancelar-reserva" no debe estar visible

Scenario: Cancelación no permitida de una reserva expirada
    Given existe una reserva en estado "Expired" con clave pública "CLAVE-CANCEL-EXPIRADA" y fecha de expiración "2026-05-01"
    And ingresa la clave pública "CLAVE-CANCEL-EXPIRADA" en el campo "#public-key-input"
    And hace clic en "#btn-buscar-reserva"
    Then el botón "#btn-cancelar-reserva" no debe estar visible

Scenario: Restricción de modificación directa de medicamentos
    Given existe una reserva en estado "Pendiente" con clave pública "CLAVE-MODIFY-TEST" y fecha de expiración "2026-07-01"
    And ingresa la clave pública "CLAVE-MODIFY-TEST" en el campo "#public-key-input"
    And hace clic en "#btn-buscar-reserva"
    Then el sistema no debe mostrar controles para modificar los medicamentos
