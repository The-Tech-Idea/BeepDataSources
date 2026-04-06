/**
 * NavigationManager — Beep DataSources documentation
 * Theme key: beep-datasources-docs-theme
 */

class NavigationManager {
    constructor() {
        const path = window.location.pathname.replace(/\\/g, '/');
        this.baseHref = path.indexOf('/providers/') !== -1 ? '../' : '';
        this.currentPage = path.split('/').pop() || 'index.html';
        this.navigationMapping = this.createNavigationMapping();
        this.init();
    }

    createNavigationMapping() {
        return {
            'index.html': { activeId: 'nav-home', openSection: null },
            'getting-started.html': { activeId: 'nav-getting-started', openSection: 'nav-start' },
            'roadmap.html': { activeId: 'nav-roadmap', openSection: 'nav-start' },
            'platform-beepdm.html': { activeId: 'nav-platform-beepdm', openSection: 'nav-platform' },
            'platform-beepservice.html': { activeId: 'nav-platform-beepservice', openSection: 'nav-platform' },
            'platform-configeditor.html': { activeId: 'nav-platform-configeditor', openSection: 'nav-platform' },
            'platform-connection.html': { activeId: 'nav-platform-connection', openSection: 'nav-platform' },
            'platform-connection-properties.html': { activeId: 'nav-platform-cp', openSection: 'nav-platform' },
            'platform-idatasource.html': { activeId: 'nav-platform-idatasource', openSection: 'nav-platform' },
            'phased-implementations.html': { activeId: 'nav-phased', openSection: 'nav-impl' },
            'impl-local-inmemory.html': { activeId: 'nav-local-inmemory', openSection: 'nav-impl' },
            'sqlite.html': { activeId: 'nav-sqlite', openSection: 'nav-impl' },
            'litedb.html': { activeId: 'nav-litedb', openSection: 'nav-impl' },
            'duckdb.html': { activeId: 'nav-duckdb', openSection: 'nav-impl' },
            'impl-rdbms.html': { activeId: 'nav-impl-rdbms', openSection: 'nav-impl' },
            'rdbms-sqlserver.html': { activeId: 'nav-rdbms-sqlserver', openSection: 'nav-impl' },
            'rdbms-postgresql.html': { activeId: 'nav-rdbms-postgresql', openSection: 'nav-impl' },
            'rdbms-mysql.html': { activeId: 'nav-rdbms-mysql', openSection: 'nav-impl' },
            'rdbms-oracle.html': { activeId: 'nav-rdbms-oracle', openSection: 'nav-impl' },
            'impl-nosql.html': { activeId: 'nav-impl-nosql', openSection: 'nav-impl' },
            'mongodb.html': { activeId: 'nav-mongodb', openSection: 'nav-impl' },
            'redis.html': { activeId: 'nav-redis', openSection: 'nav-impl' },
            'ravendb.html': { activeId: 'nav-ravendb', openSection: 'nav-impl' },
            'couchdb.html': { activeId: 'nav-couchdb', openSection: 'nav-impl' },
            'influxdb.html': { activeId: 'nav-influxdb', openSection: 'nav-impl' },
            'impl-cloud-analytics.html': { activeId: 'nav-impl-cloud', openSection: 'nav-impl' },
            'cloud-bigquery.html': { activeId: 'nav-cloud-bigquery', openSection: 'nav-impl' },
            'cloud-snowflake.html': { activeId: 'nav-cloud-snowflake', openSection: 'nav-impl' },
            'cloud-spanner.html': { activeId: 'nav-cloud-spanner', openSection: 'nav-impl' },
            'cloud-kusto.html': { activeId: 'nav-cloud-kusto', openSection: 'nav-impl' },
            'cloud-presto.html': { activeId: 'nav-cloud-presto', openSection: 'nav-impl' },
            'impl-messaging-vector.html': { activeId: 'nav-impl-msgvec', openSection: 'nav-impl' },
            'msg-kafka.html': { activeId: 'nav-msg-kafka', openSection: 'nav-impl' },
            'msg-rabbitmq.html': { activeId: 'nav-msg-rabbitmq', openSection: 'nav-impl' },
            'msg-nats.html': { activeId: 'nav-msg-nats', openSection: 'nav-impl' },
            'msg-masstransit.html': { activeId: 'nav-msg-masstransit', openSection: 'nav-impl' },
            'msg-redis-streams.html': { activeId: 'nav-msg-redis-streams', openSection: 'nav-impl' },
            'msg-google-pubsub.html': { activeId: 'nav-msg-pubsub', openSection: 'nav-impl' },
            'vector-qdrant.html': { activeId: 'nav-vec-qdrant', openSection: 'nav-impl' },
            'vector-milvus.html': { activeId: 'nav-vec-milvus', openSection: 'nav-impl' },
            'vector-chromadb.html': { activeId: 'nav-vec-chroma', openSection: 'nav-impl' },
            'impl-connectors.html': { activeId: 'nav-impl-conn', openSection: 'nav-impl' },
            'connectors-accounting.html': { activeId: 'nav-conn-accounting', openSection: 'nav-impl' },
            'connectors-business-intelligence.html': { activeId: 'nav-conn-bi', openSection: 'nav-impl' },
            'connectors-cloud-storage.html': { activeId: 'nav-conn-cloud-stor', openSection: 'nav-impl' },
            'connectors-communication.html': { activeId: 'nav-conn-comm', openSection: 'nav-impl' },
            'connectors-content-management.html': { activeId: 'nav-conn-cms', openSection: 'nav-impl' },
            'connectors-crm.html': { activeId: 'nav-conn-crm', openSection: 'nav-impl' },
            'connectors-customer-support.html': { activeId: 'nav-conn-support', openSection: 'nav-impl' },
            'connectors-ecommerce.html': { activeId: 'nav-conn-ecom', openSection: 'nav-impl' },
            'connectors-forms.html': { activeId: 'nav-conn-forms', openSection: 'nav-impl' },
            'connectors-iot.html': { activeId: 'nav-conn-iot', openSection: 'nav-impl' },
            'connectors-mail-services.html': { activeId: 'nav-conn-mail', openSection: 'nav-impl' },
            'connectors-marketing.html': { activeId: 'nav-conn-mkt', openSection: 'nav-impl' },
            'connectors-meeting-tools.html': { activeId: 'nav-conn-meet', openSection: 'nav-impl' },
            'connectors-sms.html': { activeId: 'nav-conn-sms', openSection: 'nav-impl' },
            'connectors-social-media.html': { activeId: 'nav-conn-social', openSection: 'nav-impl' },
            'connectors-task-management.html': { activeId: 'nav-conn-task', openSection: 'nav-impl' },
            'conn-salesforce.html': { activeId: 'nav-conn-ds-salesforce', openSection: 'nav-impl' },
            'conn-slack.html': { activeId: 'nav-conn-ds-slack', openSection: 'nav-impl' },
            'conn-shopify.html': { activeId: 'nav-conn-ds-shopify', openSection: 'nav-impl' },
            'conn-twitter.html': { activeId: 'nav-conn-ds-twitter', openSection: 'nav-impl' },
            'conn-hubspot.html': { activeId: 'nav-conn-ds-hubspot', openSection: 'nav-impl' },
            'conn-zendesk.html': { activeId: 'nav-conn-ds-zendesk', openSection: 'nav-impl' },
            'conn-microsoft-teams.html': { activeId: 'nav-conn-ds-teams', openSection: 'nav-impl' },
            'conn-dynamics365.html': { activeId: 'nav-conn-ds-d365', openSection: 'nav-impl' },
            'conn-mailchimp.html': { activeId: 'nav-conn-ds-mailchimp', openSection: 'nav-impl' },
            'conn-google-chat.html': { activeId: 'nav-conn-ds-gchat', openSection: 'nav-impl' },
            'conn-asana.html': { activeId: 'nav-conn-ds-asana', openSection: 'nav-impl' },
            'conn-bigcommerce.html': { activeId: 'nav-conn-ds-bc', openSection: 'nav-impl' },
            'conn-gmail.html': { activeId: 'nav-conn-ds-gmail', openSection: 'nav-impl' },
            'repo-layout.html': { activeId: 'nav-repo-layout', openSection: 'nav-ref' },
        };
    }

    getNavigationHTML() {
        const b = this.baseHref;
        return `
            <div class="logo">
                <div class="logo-icon"><i class="bi bi-database-fill-gear"></i></div>
                <div class="logo-text">
                    <h2>Beep DataSources</h2>
                    <span class="version">Help</span>
                </div>
            </div>

            <div class="search-container">
                <input type="text" class="search-input" placeholder="Search docs..."
                       oninput="searchDocs(this.value)">
            </div>

            <nav>
                <ul class="nav-menu">
                    <li><a href="${b}index.html" id="nav-home"><i class="bi bi-house-fill"></i> Home</a></li>

                    <li class="has-submenu" id="nav-start">
                        <a href="#"><i class="bi bi-rocket-takeoff"></i> Getting started</a>
                        <ul class="submenu">
                            <li><a href="${b}getting-started.html" id="nav-getting-started">How to use this help</a></li>
                            <li><a href="${b}roadmap.html" id="nav-roadmap">Roadmap &amp; phases</a></li>
                        </ul>
                    </li>

                    <li class="has-submenu" id="nav-platform">
                        <a href="#"><i class="bi bi-box-seam"></i> BeepDM platform</a>
                        <ul class="submenu">
                            <li><a href="${b}platform-beepdm.html" id="nav-platform-beepdm">Editor &amp; orchestration</a></li>
                            <li><a href="${b}platform-beepservice.html" id="nav-platform-beepservice">BeepService startup</a></li>
                            <li><a href="${b}platform-configeditor.html" id="nav-platform-configeditor">ConfigEditor</a></li>
                            <li><a href="${b}platform-connection-properties.html" id="nav-platform-cp">ConnectionProperties</a></li>
                            <li><a href="${b}platform-connection.html" id="nav-platform-connection">Connection lifecycle</a></li>
                            <li><a href="${b}platform-idatasource.html" id="nav-platform-idatasource">IDataSource contract</a></li>
                        </ul>
                    </li>

                    <li class="has-submenu" id="nav-impl">
                        <a href="#"><i class="bi bi-layers-fill"></i> Implementations (phased)</a>
                        <ul class="submenu">
                            <li><a href="${b}phased-implementations.html" id="nav-phased">Provider rollout plan</a></li>
                            <li><a href="${b}impl-local-inmemory.html" id="nav-local-inmemory">Local &amp; in-memory</a></li>
                            <li><a href="${b}providers/sqlite.html" id="nav-sqlite">SQLite</a></li>
                            <li><a href="${b}providers/litedb.html" id="nav-litedb">LiteDB</a></li>
                            <li><a href="${b}providers/duckdb.html" id="nav-duckdb">DuckDB</a></li>
                            <li><a href="${b}impl-rdbms.html" id="nav-impl-rdbms">RDBMS (overview)</a></li>
                            <li><a href="${b}providers/rdbms-sqlserver.html" id="nav-rdbms-sqlserver">SQL Server</a></li>
                            <li><a href="${b}providers/rdbms-postgresql.html" id="nav-rdbms-postgresql">PostgreSQL</a></li>
                            <li><a href="${b}providers/rdbms-mysql.html" id="nav-rdbms-mysql">MySQL</a></li>
                            <li><a href="${b}providers/rdbms-oracle.html" id="nav-rdbms-oracle">Oracle</a></li>
                            <li><a href="${b}impl-nosql.html" id="nav-impl-nosql">NoSQL (overview)</a></li>
                            <li><a href="${b}providers/mongodb.html" id="nav-mongodb">MongoDB</a></li>
                            <li><a href="${b}providers/redis.html" id="nav-redis">Redis</a></li>
                            <li><a href="${b}providers/ravendb.html" id="nav-ravendb">RavenDB</a></li>
                            <li><a href="${b}providers/couchdb.html" id="nav-couchdb">CouchDB</a></li>
                            <li><a href="${b}providers/influxdb.html" id="nav-influxdb">InfluxDB</a></li>
                            <li><a href="${b}impl-cloud-analytics.html" id="nav-impl-cloud">Cloud &amp; analytics (overview)</a></li>
                            <li><a href="${b}providers/cloud-bigquery.html" id="nav-cloud-bigquery">BigQuery</a></li>
                            <li><a href="${b}providers/cloud-snowflake.html" id="nav-cloud-snowflake">Snowflake</a></li>
                            <li><a href="${b}providers/cloud-spanner.html" id="nav-cloud-spanner">Spanner</a></li>
                            <li><a href="${b}providers/cloud-kusto.html" id="nav-cloud-kusto">Kusto (ADX)</a></li>
                            <li><a href="${b}providers/cloud-presto.html" id="nav-cloud-presto">Presto</a></li>
                            <li><a href="${b}impl-messaging-vector.html" id="nav-impl-msgvec">Messaging &amp; vector (overview)</a></li>
                            <li><a href="${b}providers/msg-kafka.html" id="nav-msg-kafka">Kafka</a></li>
                            <li><a href="${b}providers/msg-rabbitmq.html" id="nav-msg-rabbitmq">RabbitMQ</a></li>
                            <li><a href="${b}providers/msg-nats.html" id="nav-msg-nats">NATS</a></li>
                            <li><a href="${b}providers/msg-masstransit.html" id="nav-msg-masstransit">MassTransit</a></li>
                            <li><a href="${b}providers/msg-redis-streams.html" id="nav-msg-redis-streams">Redis Streams</a></li>
                            <li><a href="${b}providers/msg-google-pubsub.html" id="nav-msg-pubsub">Google Pub/Sub</a></li>
                            <li><a href="${b}providers/vector-qdrant.html" id="nav-vec-qdrant">Qdrant</a></li>
                            <li><a href="${b}providers/vector-milvus.html" id="nav-vec-milvus">Milvus</a></li>
                            <li><a href="${b}providers/vector-chromadb.html" id="nav-vec-chroma">ChromaDB</a></li>
                            <li><a href="${b}impl-connectors.html" id="nav-impl-conn">REST / SaaS connectors</a></li>
                            <li><a href="${b}impl-connectors.html#flagship-provider-pages" id="nav-impl-conn-flagship">Flagship conn-* index (table)</a></li>
                            <li><a href="${b}providers/conn-salesforce.html" id="nav-conn-ds-salesforce">Conn: Salesforce</a></li>
                            <li><a href="${b}providers/conn-slack.html" id="nav-conn-ds-slack">Conn: Slack</a></li>
                            <li><a href="${b}providers/conn-shopify.html" id="nav-conn-ds-shopify">Conn: Shopify</a></li>
                            <li><a href="${b}providers/conn-twitter.html" id="nav-conn-ds-twitter">Conn: Twitter / X</a></li>
                            <li><a href="${b}providers/conn-hubspot.html" id="nav-conn-ds-hubspot">Conn: HubSpot</a></li>
                            <li><a href="${b}providers/conn-zendesk.html" id="nav-conn-ds-zendesk">Conn: Zendesk</a></li>
                            <li><a href="${b}providers/conn-microsoft-teams.html" id="nav-conn-ds-teams">Conn: Teams</a></li>
                            <li><a href="${b}providers/conn-dynamics365.html" id="nav-conn-ds-d365">Conn: Dynamics 365</a></li>
                            <li><a href="${b}providers/conn-mailchimp.html" id="nav-conn-ds-mailchimp">Conn: Mailchimp</a></li>
                            <li><a href="${b}providers/conn-google-chat.html" id="nav-conn-ds-gchat">Conn: Google Chat</a></li>
                            <li><a href="${b}providers/conn-asana.html" id="nav-conn-ds-asana">Conn: Asana</a></li>
                            <li><a href="${b}providers/conn-bigcommerce.html" id="nav-conn-ds-bc">Conn: BigCommerce</a></li>
                            <li><a href="${b}providers/conn-gmail.html" id="nav-conn-ds-gmail">Conn: Gmail</a></li>
                            <li><a href="${b}connectors-accounting.html" id="nav-conn-accounting">Conn: Accounting</a></li>
                            <li><a href="${b}connectors-business-intelligence.html" id="nav-conn-bi">Conn: Business intelligence</a></li>
                            <li><a href="${b}connectors-cloud-storage.html" id="nav-conn-cloud-stor">Conn: Cloud storage</a></li>
                            <li><a href="${b}connectors-communication.html" id="nav-conn-comm">Conn: Communication</a></li>
                            <li><a href="${b}connectors-content-management.html" id="nav-conn-cms">Conn: Content Mgmt</a></li>
                            <li><a href="${b}connectors-crm.html" id="nav-conn-crm">Conn: CRM</a></li>
                            <li><a href="${b}connectors-customer-support.html" id="nav-conn-support">Conn: Customer support</a></li>
                            <li><a href="${b}connectors-ecommerce.html" id="nav-conn-ecom">Conn: E-commerce</a></li>
                            <li><a href="${b}connectors-forms.html" id="nav-conn-forms">Conn: Forms</a></li>
                            <li><a href="${b}connectors-iot.html" id="nav-conn-iot">Conn: IoT</a></li>
                            <li><a href="${b}connectors-mail-services.html" id="nav-conn-mail">Conn: Mail</a></li>
                            <li><a href="${b}connectors-marketing.html" id="nav-conn-mkt">Conn: Marketing</a></li>
                            <li><a href="${b}connectors-meeting-tools.html" id="nav-conn-meet">Conn: Meetings</a></li>
                            <li><a href="${b}connectors-sms.html" id="nav-conn-sms">Conn: SMS</a></li>
                            <li><a href="${b}connectors-social-media.html" id="nav-conn-social">Conn: Social media</a></li>
                            <li><a href="${b}connectors-task-management.html" id="nav-conn-task">Conn: Tasks</a></li>
                        </ul>
                    </li>

                    <li class="has-submenu" id="nav-ref">
                        <a href="#"><i class="bi bi-folder2-open"></i> Reference</a>
                        <ul class="submenu">
                            <li><a href="${b}repo-layout.html" id="nav-repo-layout">Repository layout</a></li>
                        </ul>
                    </li>
                </ul>
            </nav>
        `;
    }

    init() {
        document.addEventListener('DOMContentLoaded', () => {
            this.loadNavigation();
            this.applyStoredTheme();
        });
    }

    loadNavigation() {
        const sidebar = document.getElementById('sidebar');
        if (!sidebar) { console.error('Sidebar element not found'); return; }
        sidebar.innerHTML = this.getNavigationHTML();
        this.setupNavigation();
    }

    setupNavigation() {
        this.setActiveStates();
        this.setupSubmenuToggles();
        if (this.currentPage === 'impl-connectors.html') {
            window.addEventListener('hashchange', () => this.setActiveStates());
        }
    }

    setActiveStates() {
        const mapping = this.navigationMapping[this.currentPage];
        if (!mapping) return;

        const sidebar = document.getElementById('sidebar');
        if (sidebar) {
            sidebar.querySelectorAll('nav a.active').forEach((a) => a.classList.remove('active'));
        }

        let activeId = mapping.activeId;
        if (this.currentPage === 'impl-connectors.html' && window.location.hash === '#flagship-provider-pages') {
            activeId = 'nav-impl-conn-flagship';
        }

        if (activeId) {
            const el = document.getElementById(activeId);
            if (el) el.classList.add('active');
        }

        if (mapping.openSection) {
            const section = document.getElementById(mapping.openSection);
            if (section) section.classList.add('open');
        }
    }

    setupSubmenuToggles() {
        document.querySelectorAll('.has-submenu > a').forEach(item => {
            item.addEventListener('click', function (e) {
                e.preventDefault();
                this.parentElement.classList.toggle('open');
            });
        });
    }

    applyStoredTheme() {
        const stored = localStorage.getItem('beep-datasources-docs-theme');
        if (stored === 'dark') {
            document.documentElement.setAttribute('data-theme', 'dark');
            const icon = document.getElementById('theme-icon');
            if (icon) { icon.classList.remove('bi-sun-fill'); icon.classList.add('bi-moon-fill'); }
        }
    }
}

function toggleTheme() {
    const html = document.documentElement;
    const isDark = html.getAttribute('data-theme') === 'dark';
    const icon = document.getElementById('theme-icon');

    if (isDark) {
        html.removeAttribute('data-theme');
        localStorage.setItem('beep-datasources-docs-theme', 'light');
        if (icon) { icon.classList.remove('bi-moon-fill'); icon.classList.add('bi-sun-fill'); }
    } else {
        html.setAttribute('data-theme', 'dark');
        localStorage.setItem('beep-datasources-docs-theme', 'dark');
        if (icon) { icon.classList.remove('bi-sun-fill'); icon.classList.add('bi-moon-fill'); }
    }
}

function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    if (sidebar) sidebar.classList.toggle('open');
}

function searchDocs(query) {
    const q = query.toLowerCase().trim();
    document.querySelectorAll('.nav-menu a').forEach(link => {
        const text = link.textContent.toLowerCase();
        const li = link.closest('li');
        if (li) li.style.display = (!q || text.includes(q)) ? '' : 'none';
    });
    if (!q) {
        document.querySelectorAll('.nav-menu li').forEach(li => li.style.display = '');
    }
}

const navManager = new NavigationManager();
