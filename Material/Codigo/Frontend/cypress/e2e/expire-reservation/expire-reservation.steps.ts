import { Given, When, Then } from 'cypress-cucumber-preprocessor/steps';

let reservations: any[] = [];

beforeEach(() => {
  reservations = [];
});

Given('existe una reserva pendiente creada hace {int} días con clave pública {string}',
  (daysAgo: number, publicKey: string) => {
    const reservationDate = new Date();
    reservationDate.setDate(reservationDate.getDate() - daysAgo);

    const isExpired = daysAgo > 30;

    reservations.push({
      id: reservations.length + 1,
      code: `RES-EXP-00${reservations.length + 1}`,
      publicKey: publicKey,
      userEmail: 'cliente@example.com',
      reservationDate: reservationDate.toISOString(),
      expirationDate: new Date(reservationDate.getTime() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
      status: isExpired ? 'Expired' : 'Pending',
      prescriptionUploaded: false,
      details: [
        { id: 1, drugCode: 'PAR-500', drugName: 'Paracetamol 500mg', quantity: 3, requiresPrescription: false }
      ]
    });
  }
);

Given('existe una reserva confirmada creada hace {int} días con clave pública {string}',
  (daysAgo: number, publicKey: string) => {
    const reservationDate = new Date();
    reservationDate.setDate(reservationDate.getDate() - daysAgo);

    const isExpired = daysAgo > 30;

    reservations.push({
      id: reservations.length + 1,
      code: `RES-CONF-00${reservations.length + 1}`,
      publicKey: publicKey,
      userEmail: 'cliente@example.com',
      reservationDate: reservationDate.toISOString(),
      expirationDate: new Date(reservationDate.getTime() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
      status: isExpired ? 'Expired' : 'Confirmed',
      prescriptionUploaded: false,
      details: [
        { id: 1, drugCode: 'PAR-500', drugName: 'Paracetamol 500mg', quantity: 3, requiresPrescription: false }
      ]
    });
  }
);

Given('existe una reserva cancelada creada hace {int} días con clave pública {string}',
  (daysAgo: number, publicKey: string) => {
    const reservationDate = new Date();
    reservationDate.setDate(reservationDate.getDate() - daysAgo);

    reservations.push({
      id: reservations.length + 1,
      code: `RES-CANC-00${reservations.length + 1}`,
      publicKey: publicKey,
      userEmail: 'cliente@example.com',
      reservationDate: reservationDate.toISOString(),
      expirationDate: new Date(reservationDate.getTime() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
      status: 'Cancelled',
      prescriptionUploaded: false,
      details: [
        { id: 1, drugCode: 'PAR-500', drugName: 'Paracetamol 500mg', quantity: 3, requiresPrescription: false }
      ]
    });
  }
);

Given('el cliente visita la página {string}', (url: string) => {
  cy.visit(`http://localhost:4200${url}`);
});

Given('ingresa la clave pública {string} en el campo {string}', (publicKey: string, selector: string) => {
  cy.get(selector).clear().type(publicKey);
});

Given('busca la reserva', () => {
  cy.intercept('GET', '**/api/reservation*', {
    statusCode: 200,
    body: reservations[0]
  }).as('getReservation');
  cy.get('#btn-buscar-reserva').click();
  cy.wait('@getReservation');
});

When('el cliente busca su reserva con clave pública {string}', (publicKey: string) => {
  const reservation = reservations.find(r => r.publicKey === publicKey);
  cy.intercept('GET', '**/api/reservation*', {
    statusCode: 200,
    body: reservation
  }).as('getReservation');
  cy.visit('http://localhost:4200/reservations');
  cy.get('#public-key-input').clear().type(publicKey);
  cy.get('#btn-buscar-reserva').click();
  cy.wait('@getReservation');
});

Then('el sistema debe mostrar la reserva como {string}', (status: string) => {
  cy.get('[data-cy=reservation-status]').should('contain', status);
});

When('el empleado solicita todas las reservas pendientes', () => {
  const pendingReservations = reservations.filter(r => r.status === 'Pending');

  localStorage.setItem('login', JSON.stringify({ role: 'Employee', token: 'jwt-simulado-para-test' }));

  cy.intercept('GET', '**/api/reservation/pending', {
    statusCode: 200,
    body: pendingReservations
  }).as('getAllPending');
  cy.visit('http://localhost:4200/employee/validate-reservations');
  cy.wait('@getAllPending', { timeout: 10000 });
});

Then('solo debe aparecer la reserva con código {string}', (code: string) => {
  cy.get('[data-cy=reservation-row]').should('have.length', 1);
  cy.get('[data-cy=reservation-row]').should('contain', code);
});

Then('el sistema debe mostrar el mensaje {string}', (message: string) => {
  cy.get('[data-cy=reservation-message]').should('contain', message);
});

Then('el botón {string} no debe estar visible', (selector: string) => {
  cy.get(selector).should('not.exist');
});
